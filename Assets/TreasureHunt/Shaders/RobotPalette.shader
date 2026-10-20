Shader "Treasure Hunt/Robot Palette"
{
    Properties
    {
        [MainTexture] _BaseMap("Robot Diffuse",2D)="white" {}
        [MainColor] _BaseColor("Tint",Color)=(1,1,1,1)
        [Toggle] _EnemyRecolor("Red Armor",Float)=0
        [Normal] _BumpMap("Normal",2D)="bump" {}
        _BumpScale("Normal Strength",Float)=.8
        _MetallicGlossMap("Metallic",2D)="white" {}
        _Metallic("Metallic Strength",Range(0,1))=.45
        _Smoothness("Smoothness",Range(0,1))=.4
        _OcclusionMap("Occlusion",2D)="white" {}
        _OcclusionStrength("Occlusion Strength",Range(0,1))=1
        _EmissionMap("Emission",2D)="black" {}
        _EmissionColor("Emission Color",Color)=(.08,.08,.08,1)
        [HideInInspector] _Cutoff("Cutoff",Float)=.5
        [HideInInspector] _Surface("Surface",Float)=0
        [HideInInspector] _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LitPassVertex
            #pragma fragment RobotFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _EMISSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #define _ADDITIONAL_LIGHTS 1
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            half _EnemyRecolor;
            half4 RobotFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                SurfaceData surface;
                InitializeStandardLitSurfaceData(input.uv,surface);
                half3 original=surface.albedo;
                half blueMask=saturate((original.b-max(original.r,original.g)*.9h)*9.0h);
                surface.albedo=lerp(original,original.bgr,blueMask*_EnemyRecolor);
                InputData lighting;
                InitializeInputData(input,surface.normalTS,lighting);
                InitializeBakedGIData(input,lighting);
                half4 color=UniversalFragmentPBR(lighting,surface);
                color.rgb=MixFog(color.rgb,lighting.fogCoord);
                return half4(color.rgb,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
