﻿using GoThrough.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace GoThrough
{
    [RequireComponent(typeof(Camera))]
    public class PortalRenderer : MonoBehaviour
    {
        [SerializeField]
        private int maxRecursionDepth = 5;

        [SerializeField]
        private int maxTextureAllocations = 100;

        public int MaxTextureAllocations
        {
            get => this.maxTextureAllocations;
            set
            {
                this.maxTextureAllocations = value;
                this.RenderTexturePool.MaxTextureAllocations = value;
            }
        }

        public int MaxRecursionDepth
        {
            get => this.maxRecursionDepth;
            set => this.maxRecursionDepth = value;
        }

        public RenderTexturePool RenderTexturePool { get; private set; }

        public Camera BaseCamera { get; private set; }

        public Camera PortalCamera { get; private set; }

        private int CurrentWidth => this.BaseCamera.targetTexture ? this.BaseCamera.targetTexture.width : Screen.width;
        private int CurrentHeight => this.BaseCamera.targetTexture ? this.BaseCamera.targetTexture.height : Screen.height;
        
        private int previousWidth;
        private int previousHeight;

        private void Awake()
        {
            this.BaseCamera = this.GetComponent<Camera>();

            var cameraObject = new GameObject("PortalCamera");
            cameraObject.transform.SetParent(this.transform);
            this.PortalCamera = cameraObject.AddComponent<Camera>();
            this.PortalCamera.enabled = false;
        }

        private void Start()
        {
            this.previousWidth = this.CurrentWidth;
            this.previousHeight = this.CurrentHeight;
            this.RenderTexturePool = RenderTexturePool.Create(this.previousWidth, this.previousHeight, this.maxTextureAllocations);
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += this.RenderPipelineManager_beginCameraRendering;
            RenderPipelineManager.endCameraRendering += this.RenderPipelineManager_endCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= this.RenderPipelineManager_beginCameraRendering;
            RenderPipelineManager.endCameraRendering -= this.RenderPipelineManager_endCameraRendering;
        }

        private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam == this.BaseCamera)
            {
                if (this.CurrentWidth != this.previousWidth || this.CurrentHeight != this.previousHeight)
                    this.RenderTexturePool.SetResolution(this.CurrentWidth, this.CurrentHeight);

                this.PortalCamera.cullingMask = this.BaseCamera.cullingMask;
                this.PortalCamera.projectionMatrix = this.BaseCamera.projectionMatrix;

                foreach (var portal in PortalManager.Instance.Portals)
                    portal.SetupScreen(cam);

                var graph = new VisibilityGraph(this);
                graph.Render(ctx);
            }
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext arg1, Camera arg2)
        {
            this.RenderTexturePool.ReleaseAllRenderTextures();
        }
    }
}