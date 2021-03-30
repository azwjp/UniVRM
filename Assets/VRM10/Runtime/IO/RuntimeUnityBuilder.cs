﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VrmLib;


namespace UniVRM10
{
    /// <summary>
    /// VrmLib.Model から UnityPrefab を構築する
    /// </summary>
    public class RuntimeUnityBuilder : UniGLTF.ImporterContext
    {
        readonly Model m_model;

        /// <summary>
        /// VrmLib.Model の オブジェクトと UnityEngine.Object のマッピングを記録する
        /// </summary>
        /// <returns></returns>
        readonly ModelAsset m_asset = new ModelAsset();

        public ModelAsset Asset => m_asset;

        UniGLTF.Extensions.VRMC_vrm.VRMC_vrm m_vrm;

        public RuntimeUnityBuilder(UniGLTF.GltfParser parser, IEnumerable<(string, UnityEngine.Object)> externalObjectMap = null) : base(parser, externalObjectMap)
        {
            m_model = VrmLoader.CreateVrmModel(parser);

            // for `VRMC_materials_mtoon`
            this.GltfMaterialImporter.GltfMaterialParamProcessors.Insert(0, Vrm10MToonMaterialImporter.TryCreateParam);

            if (!UniGLTF.Extensions.VRMC_vrm.GltfDeserializer.TryGet(parser.GLTF.extensions, out m_vrm))
            {
                throw new Exception("VRMC_vrm is not found");
            }

            // assign humanoid bones
            if (m_vrm.Humanoid != null)
            {
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Hips, HumanoidBones.hips);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftUpperLeg, HumanoidBones.leftUpperLeg);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightUpperLeg, HumanoidBones.rightUpperLeg);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftLowerLeg, HumanoidBones.leftLowerLeg);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightLowerLeg, HumanoidBones.rightLowerLeg);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftFoot, HumanoidBones.leftFoot);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightFoot, HumanoidBones.rightFoot);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Spine, HumanoidBones.spine);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Chest, HumanoidBones.chest);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Neck, HumanoidBones.neck);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Head, HumanoidBones.head);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftShoulder, HumanoidBones.leftShoulder);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightShoulder, HumanoidBones.rightShoulder);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftUpperArm, HumanoidBones.leftUpperArm);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightUpperArm, HumanoidBones.rightUpperArm);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftLowerArm, HumanoidBones.leftLowerArm);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightLowerArm, HumanoidBones.rightLowerArm);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftHand, HumanoidBones.leftHand);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightHand, HumanoidBones.rightHand);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftToes, HumanoidBones.leftToes);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightToes, HumanoidBones.rightToes);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftEye, HumanoidBones.leftEye);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightEye, HumanoidBones.rightEye);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.Jaw, HumanoidBones.jaw);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftThumbProximal, HumanoidBones.leftThumbProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftThumbIntermediate, HumanoidBones.leftThumbIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftThumbDistal, HumanoidBones.leftThumbDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftIndexProximal, HumanoidBones.leftIndexProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftIndexIntermediate, HumanoidBones.leftIndexIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftIndexDistal, HumanoidBones.leftIndexDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftMiddleProximal, HumanoidBones.leftMiddleProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftMiddleIntermediate, HumanoidBones.leftMiddleIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftMiddleDistal, HumanoidBones.leftMiddleDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftRingProximal, HumanoidBones.leftRingProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftRingIntermediate, HumanoidBones.leftRingIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftRingDistal, HumanoidBones.leftRingDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftLittleProximal, HumanoidBones.leftLittleProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftLittleIntermediate, HumanoidBones.leftLittleIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.LeftLittleDistal, HumanoidBones.leftLittleDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightThumbProximal, HumanoidBones.rightThumbProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightThumbIntermediate, HumanoidBones.rightThumbIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightThumbDistal, HumanoidBones.rightThumbDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightIndexProximal, HumanoidBones.rightIndexProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightIndexIntermediate, HumanoidBones.rightIndexIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightIndexDistal, HumanoidBones.rightIndexDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightMiddleProximal, HumanoidBones.rightMiddleProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightMiddleIntermediate, HumanoidBones.rightMiddleIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightMiddleDistal, HumanoidBones.rightMiddleDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightRingProximal, HumanoidBones.rightRingProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightRingIntermediate, HumanoidBones.rightRingIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightRingDistal, HumanoidBones.rightRingDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightLittleProximal, HumanoidBones.rightLittleProximal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightLittleIntermediate, HumanoidBones.rightLittleIntermediate);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.RightLittleDistal, HumanoidBones.rightLittleDistal);
                AssignHumanoid(m_model.Nodes, m_vrm.Humanoid.HumanBones.UpperChest, HumanoidBones.upperChest);
            }
        }

        static void AssignHumanoid(List<Node> nodes, UniGLTF.Extensions.VRMC_vrm.HumanBone humanBone, VrmLib.HumanoidBones key)
        {
            if (humanBone != null && humanBone.Node.HasValue)
            {
                nodes[humanBone.Node.Value].HumanoidBone = key;
            }
        }

        /// <summary>
        /// VrmLib.Model から 構築する
        /// </summary>
        /// <param name="MeasureTime"></param>
        /// <returns></returns>
        protected override async Task LoadGeometryAsync(IAwaitCaller awaitCaller, Func<string, IDisposable> MeasureTime)
        {
            // fill assets
            for (int i = 0; i < m_model.Materials.Count; ++i)
            {
                var src = m_model.Materials[i];
                var dst = MaterialFactory.Materials[i].Asset;
            }

            await awaitCaller.NextFrame();

            // mesh
            for (int i = 0; i < m_model.MeshGroups.Count; ++i)
            {
                var src = m_model.MeshGroups[i];
                if (src.Meshes.Count == 1)
                {
                    // submesh 方式
                    var mesh = new UnityEngine.Mesh();
                    mesh.name = src.Name;
                    mesh.LoadMesh(src.Meshes[0], src.Skin);
                    m_asset.Map.Meshes.Add(src, mesh);
                    m_asset.Meshes.Add(mesh);
                    Meshes.Add(new MeshWithMaterials
                    {
                        Mesh = mesh,
                        Materials = src.Meshes[0].Submeshes.Select(x => MaterialFactory.Materials[x.Material].Asset).ToArray(),
                    });
                }
                else
                {
                    // 頂点バッファの連結が必用
                    throw new NotImplementedException();
                }

                await awaitCaller.NextFrame();
            }

            // node: recursive
            CreateNodes(m_model.Root, null, m_asset.Map.Nodes);
            await awaitCaller.NextFrame();

            if (Root == null)
            {
                Root = m_asset.Map.Nodes[m_model.Root];
            }
            else
            {
                // replace
                var modelRoot = m_asset.Map.Nodes[m_model.Root];
                foreach (Transform child in modelRoot.transform)
                {
                    child.SetParent(Root.transform, true);
                }
                m_asset.Map.Nodes[m_model.Root] = Root;
            }
            m_asset.Root = m_asset.Map.Nodes[m_model.Root];
            await awaitCaller.NextFrame();

            // renderer
            var map = m_asset.Map;
            foreach (var (node, go) in map.Nodes)
            {
                if (node.MeshGroup is null)
                {
                    continue;
                }

                if (node.MeshGroup.Meshes.Count > 1)
                {
                    throw new NotImplementedException("invalid isolated vertexbuffer");
                }

                var renderer = CreateRenderer(node, go, map, MaterialFactory.Materials);
                map.Renderers.Add(node, renderer);
                m_asset.Renderers.Add(renderer);
                await awaitCaller.NextFrame();
            }
        }

        protected override async Task OnLoadHierarchy(IAwaitCaller awaitCaller, Func<string, IDisposable> MeasureTime)
        {
            Root.name = "VRM1";

            // humanoid
            var humanoid = Root.AddComponent<MeshUtility.Humanoid>();
            humanoid.AssignBones(m_asset.Map.Nodes.Select(x => (ToUnity(x.Key.HumanoidBone.GetValueOrDefault()), x.Value.transform)));
            m_asset.HumanoidAvatar = humanoid.CreateAvatar();
            m_asset.HumanoidAvatar.name = "VRM1";
            var animator = m_asset.Root.AddComponent<Animator>();
            animator.avatar = m_asset.HumanoidAvatar;

            // VrmController
            var controller = Root.AddComponent<VRM10Controller>();

            // meta
            if (m_vrm.Meta != null)
            {

            }

            // firstPerson

            // expression

            // lookat

            // springBone

            // constraint

            await awaitCaller.NextFrame();
        }

        public static HumanBodyBones ToUnity(VrmLib.HumanoidBones bone)
        {
            if (bone == VrmLib.HumanoidBones.unknown)
            {
                return HumanBodyBones.LastBone;
            }
            return VrmLib.EnumUtil.Cast<HumanBodyBones>(bone);
        }

        /// <summary>
        /// ヒエラルキーを再帰的に構築する
        /// <summary>
        public static void CreateNodes(VrmLib.Node node, GameObject parent, Dictionary<VrmLib.Node, GameObject> nodes)
        {
            GameObject go = new GameObject(node.Name);
            go.transform.SetPositionAndRotation(node.Translation.ToUnityVector3(), node.Rotation.ToUnityQuaternion());
            nodes.Add(node, go);
            if (parent != null)
            {
                go.transform.SetParent(parent.transform);
            }

            if (node.Children.Count > 0)
            {
                for (int n = 0; n < node.Children.Count; n++)
                {
                    CreateNodes(node.Children[n], go, nodes);
                }
            }
        }

        /// <summary>
        /// MeshFilter + MeshRenderer もしくは SkinnedMeshRenderer を構築する
        /// </summary>
        public static Renderer CreateRenderer(VrmLib.Node node, GameObject go, ModelMap map,
            IReadOnlyList<VRMShaders.MaterialFactory.MaterialLoadInfo> materialLoadInfos)
        {
            var mesh = node.MeshGroup.Meshes[0];

            Renderer renderer = null;
            var hasBlendShape = mesh.MorphTargets.Any();
            if (node.MeshGroup.Skin != null || hasBlendShape)
            {
                var skinnedMeshRenderer = go.AddComponent<SkinnedMeshRenderer>();
                renderer = skinnedMeshRenderer;
                skinnedMeshRenderer.sharedMesh = map.Meshes[node.MeshGroup];
                if (node.MeshGroup.Skin != null)
                {
                    skinnedMeshRenderer.bones = node.MeshGroup.Skin.Joints.Select(x => map.Nodes[x].transform).ToArray();
                    if (node.MeshGroup.Skin.Root != null)
                    {
                        skinnedMeshRenderer.rootBone = map.Nodes[node.MeshGroup.Skin.Root].transform;
                    }
                }
            }
            else
            {
                var meshFilter = go.AddComponent<MeshFilter>();
                renderer = go.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = map.Meshes[node.MeshGroup];
            }
            var materials = mesh.Submeshes.Select(x => materialLoadInfos[x.Material].Asset).ToArray();
            renderer.sharedMaterials = materials;

            return renderer;
        }
    }
}
