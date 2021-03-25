﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using VRMShaders;


namespace UniGLTF
{
    [Flags]
    public enum TextureLoadFlags
    {
        None = 0,
        Used = 1,
        External = 1 << 1,
    }

    public struct TextureLoadInfo
    {
        public readonly Texture2D Texture;
        public readonly TextureLoadFlags Flags;
        public bool IsUsed => Flags.HasFlag(TextureLoadFlags.Used);
        public bool IsExternal => Flags.HasFlag(TextureLoadFlags.External);

        public bool IsSubAsset => IsUsed && !IsExternal;

        public TextureLoadInfo(Texture2D texture, bool used, bool isExternal)
        {
            Texture = texture;
            var flags = TextureLoadFlags.None;
            if (used)
            {
                flags |= TextureLoadFlags.Used;
            }
            if (isExternal)
            {
                flags |= TextureLoadFlags.External;
            }
            Flags = flags;
        }
    }

    public delegate Task<Texture2D> GetTextureAsyncFunc(IAwaitCaller awaitCaller, glTF gltf, TextureImportParam param);
    public class TextureFactory : IDisposable
    {
        glTF m_gltf;
        IStorage m_storage;

        public readonly Dictionary<string, Texture2D> ExternalMap;

        public TextureFactory(glTF gltf, IStorage storage, IEnumerable<(string, UnityEngine.Object)> externalMap)
        {
            m_gltf = gltf;
            m_storage = storage;

            if (externalMap != null)
            {
                ExternalMap = externalMap
                    .Select(kv => (kv.Item1, kv.Item2 as Texture2D))
                    .Where(kv => kv.Item2 != null)
                    .ToDictionary(kv => kv.Item1, kv => kv.Item2);
            }
        }

        public void Dispose()
        {
            Action<UnityEngine.Object> destroy = UnityResourceDestroyer.DestroyResource();
            foreach (var kv in m_textureCache)
            {
                if (!kv.Value.IsExternal)
                {
#if VRM_DEVELOP
                    // Debug.Log($"Destroy {kv.Value.Texture}");
#endif
                    destroy(kv.Value.Texture);
                }
            }
            m_textureCache.Clear();
        }

        /// <summary>
        /// 所有権(Dispose権)を移譲する
        /// </summary>
        /// <param name="take"></param>
        public void TransferOwnership(TakeOwnershipFunc take)
        {
            var keys = new List<string>();
            foreach (var x in m_textureCache)
            {
                if (x.Value.IsUsed && !x.Value.IsExternal)
                {
                    // マテリアルから参照されていて
                    // 外部のAssetからロードしていない。
                    if (take(x.Value.Texture))
                    {
                        keys.Add(x.Key);
                    }
                }
            }
            foreach (var x in keys)
            {
                m_textureCache.Remove(x);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="string"></typeparam>
        /// <typeparam name="TextureLoadInfo"></typeparam>
        /// <returns></returns>
        Dictionary<string, TextureLoadInfo> m_textureCache = new Dictionary<string, TextureLoadInfo>();

        public IEnumerable<TextureLoadInfo> Textures => m_textureCache.Values;

        static Byte[] ToArray(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null)
            {
                return new byte[] { };
            }
            else if (bytes.Offset == 0 && bytes.Count == bytes.Array.Length)
            {
                return bytes.Array;
            }
            else
            {
                Byte[] result = new byte[bytes.Count];
                Buffer.BlockCopy(bytes.Array, bytes.Offset, result, 0, result.Length);
                return result;
            }
        }

        async Task<TextureLoadInfo> GetOrCreateBaseTexture(IAwaitCaller awaitCaller, TextureImportParam param, GetTextureBytesAsync getTextureBytesAsync, RenderTextureReadWrite colorSpace, bool used)
        {
            var name = param.GltfName;
            if (m_textureCache.TryGetValue(name, out TextureLoadInfo cacheInfo))
            {
                return cacheInfo;
            }

            // not found. load new
            var imageBytes = await getTextureBytesAsync();

            //
            // texture from image(png etc) bytes
            //
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false, colorSpace == RenderTextureReadWrite.Linear);
            texture.name = name;
            if (imageBytes != null)
            {
                texture.LoadImage(imageBytes);
            }

            SetSampler(texture, param);

            cacheInfo = new TextureLoadInfo(texture, used, false);
            m_textureCache.Add(name, cacheInfo);
            return cacheInfo;
        }

        public static void SetSampler(Texture2D texture, TextureImportParam param)
        {
            if (texture == null)
            {
                return;
            }

            foreach (var (key, value) in param.Sampler.WrapModes)
            {
                switch (key)
                {
                    case SamplerWrapType.All:
                        texture.wrapMode = value;
                        break;

                    case SamplerWrapType.U:
                        texture.wrapModeU = value;
                        break;

                    case SamplerWrapType.V:
                        texture.wrapModeV = value;
                        break;

                    case SamplerWrapType.W:
                        texture.wrapModeW = value;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            texture.filterMode = param.Sampler.FilterMode;
        }

        /// <summary>
        /// テクスチャーをロード、必要であれば変換して返す。
        /// 同じものはキャッシュを返す
        /// </summary>
        /// <param name="texture_type">変換の有無を判断する: METALLIC_GLOSS_PROP</param>
        /// <param name="roughnessFactor">METALLIC_GLOSS_PROPの追加パラメーター</param>
        /// <param name="indices">gltf の texture index</param>
        /// <returns></returns>
        public async Task<Texture2D> GetTextureAsync(IAwaitCaller awaitCaller, glTF gltf, TextureImportParam param)
        {
            //
            // ExtractKey で External とのマッチングを試みる
            // 
            // Normal => GltfName
            // Standard => ConvertedName
            // sRGB => GltfName 
            // Linear => GltfName 
            //
            if (param.Index0 != null && ExternalMap != null)
            {
                if (ExternalMap.TryGetValue(param.ExtractKey, out Texture2D external))
                {
                    return external;
                }
            }

            switch (param.TextureType)
            {
                case TextureImportTypes.NormalMap:
                    // Runtime/SubAsset 用に変換する
                    {
                        if (!m_textureCache.TryGetValue(param.ConvertedName, out TextureLoadInfo info))
                        {
                            var baseTexture = await GetOrCreateBaseTexture(awaitCaller, param, param.Index0, RenderTextureReadWrite.Linear, false);
                            var converted = NormalConverter.Import(baseTexture.Texture);
                            converted.name = param.ConvertedName;
                            info = new TextureLoadInfo(converted, true, false);
                            m_textureCache.Add(converted.name, info);
                        }
                        return info.Texture;
                    }

                case TextureImportTypes.StandardMap:
                    // 変換する
                    {
                        if (!m_textureCache.TryGetValue(param.ConvertedName, out TextureLoadInfo info))
                        {
                            TextureLoadInfo baseTexture = default;
                            if (param.Index0!=null)
                            {
                                baseTexture = await GetOrCreateBaseTexture(awaitCaller, param, param.Index0, RenderTextureReadWrite.Linear, false);
                            }
                            TextureLoadInfo occlusionBaseTexture = default;
                            if (param.Index1!=null)
                            {
                                occlusionBaseTexture = await GetOrCreateBaseTexture(awaitCaller, param, param.Index1, RenderTextureReadWrite.Linear, false);
                            }
                            var converted = OcclusionMetallicRoughnessConverter.Import(baseTexture.Texture, param.MetallicFactor, param.RoughnessFactor, occlusionBaseTexture.Texture);
                            converted.name = param.ConvertedName;
                            info = new TextureLoadInfo(converted, true, false);
                            m_textureCache.Add(converted.name, info);
                        }
                        return info.Texture;
                    }

                default:
                    {
                        var baseTexture = await GetOrCreateBaseTexture(awaitCaller, param, param.Index0, RenderTextureReadWrite.sRGB, true);
                        return baseTexture.Texture;
                    }
            }

            throw new NotImplementedException();
        }

        public static TextureImportParam CreateSRGB(GltfParser parser, int textureIndex, Vector2 offset, Vector2 scale)
        {
            var name = CreateNameExt(parser.GLTF, textureIndex, TextureImportTypes.sRGB);
            var sampler = CreateSampler(parser.GLTF, textureIndex);
            GetTextureBytesAsync getTextureBytesAsync = () => Task.FromResult(ToArray(parser.GLTF.GetImageBytesFromTextureIndex(parser.Storage, textureIndex)));
            return new TextureImportParam(name, offset, scale, sampler, TextureImportTypes.sRGB, default, default, getTextureBytesAsync, default, default, default, default, default);
        }

        public static TextureImportParam CreateNormal(GltfParser parser, int textureIndex, Vector2 offset, Vector2 scale)
        {
            var name = CreateNameExt(parser.GLTF, textureIndex, TextureImportTypes.NormalMap);
            var sampler = CreateSampler(parser.GLTF, textureIndex);
            GetTextureBytesAsync getTextureBytesAsync = () => Task.FromResult(ToArray(parser.GLTF.GetImageBytesFromTextureIndex(parser.Storage, textureIndex)));
            return new TextureImportParam(name, offset, scale, sampler, TextureImportTypes.NormalMap, default, default, getTextureBytesAsync, default, default, default, default, default);
        }

        public static TextureImportParam CreateStandard(GltfParser parser, int? metallicRoughnessTextureIndex, int? occlusionTextureIndex, Vector2 offset, Vector2 scale, float metallicFactor, float roughnessFactor)
        {
            TextureImportName name = default;

            GetTextureBytesAsync getMetallicRoughnessAsync = default;
            SamplerParam sampler = default;
            if (metallicRoughnessTextureIndex.HasValue)
            {
                name = CreateNameExt(parser.GLTF, metallicRoughnessTextureIndex.Value, TextureImportTypes.StandardMap);
                sampler = CreateSampler(parser.GLTF, metallicRoughnessTextureIndex.Value);
                getMetallicRoughnessAsync = () => Task.FromResult(ToArray(parser.GLTF.GetImageBytesFromTextureIndex(parser.Storage, metallicRoughnessTextureIndex.Value)));
            }

            GetTextureBytesAsync getOcclusionAsync = default;
            if (occlusionTextureIndex.HasValue)
            {
                if(string.IsNullOrEmpty(name.GltfName)){
                    name = CreateNameExt(parser.GLTF, occlusionTextureIndex.Value, TextureImportTypes.StandardMap);
                }
                sampler = CreateSampler(parser.GLTF, occlusionTextureIndex.Value);
                getOcclusionAsync = () => Task.FromResult(ToArray(parser.GLTF.GetImageBytesFromTextureIndex(parser.Storage, occlusionTextureIndex.Value)));
            }

            return new TextureImportParam(name, offset, scale, sampler, TextureImportTypes.StandardMap, metallicFactor, roughnessFactor, getMetallicRoughnessAsync, getOcclusionAsync, default, default, default, default);
        }

        public static TextureImportName CreateNameExt(glTF gltf, int textureIndex, TextureImportTypes textureType)
        {
            if (textureIndex < 0 || textureIndex >= gltf.textures.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            var gltfTexture = gltf.textures[textureIndex];
            if (gltfTexture.source < 0 || gltfTexture.source >= gltf.images.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            var gltfImage = gltf.images[gltfTexture.source];
            return new TextureImportName(textureType, gltfTexture.name, gltfImage.GetExt(), gltfImage.uri);
        }

        public static SamplerParam CreateSampler(glTF gltf, int index)
        {
            var gltfTexture = gltf.textures[index];
            if (gltfTexture.sampler < 0 || gltfTexture.sampler >= gltf.samplers.Count)
            {
                // default
                return new SamplerParam
                {
                    FilterMode = FilterMode.Bilinear,
                    WrapModes = new (SamplerWrapType, TextureWrapMode)[] { },
                };
            }

            var gltfSampler = gltf.samplers[gltfTexture.sampler];
            return new SamplerParam
            {
                WrapModes = GetUnityWrapMode(gltfSampler).ToArray(),
                FilterMode = ImportFilterMode(gltfSampler.minFilter),
            };
        }

        public static IEnumerable<(SamplerWrapType, TextureWrapMode)> GetUnityWrapMode(glTFTextureSampler sampler)
        {
            if (sampler.wrapS == sampler.wrapT)
            {
                switch (sampler.wrapS)
                {
                    case glWrap.NONE: // default
                        yield return (SamplerWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return (SamplerWrapType.All, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return (SamplerWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return (SamplerWrapType.All, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (sampler.wrapS)
                {
                    case glWrap.NONE: // default
                        yield return (SamplerWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return (SamplerWrapType.U, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return (SamplerWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return (SamplerWrapType.U, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                switch (sampler.wrapT)
                {
                    case glWrap.NONE: // default
                        yield return (SamplerWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case glWrap.CLAMP_TO_EDGE:
                        yield return (SamplerWrapType.V, TextureWrapMode.Clamp);
                        break;

                    case glWrap.REPEAT:
                        yield return (SamplerWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case glWrap.MIRRORED_REPEAT:
                        yield return (SamplerWrapType.V, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static FilterMode ImportFilterMode(glFilter filterMode)
        {
            switch (filterMode)
            {
                case glFilter.NEAREST:
                case glFilter.NEAREST_MIPMAP_LINEAR:
                case glFilter.NEAREST_MIPMAP_NEAREST:
                    return FilterMode.Point;

                case glFilter.NONE:
                case glFilter.LINEAR:
                case glFilter.LINEAR_MIPMAP_NEAREST:
                    return FilterMode.Bilinear;

                case glFilter.LINEAR_MIPMAP_LINEAR:
                    return FilterMode.Trilinear;

                default:
                    throw new NotImplementedException();
            }
        }

    }
}