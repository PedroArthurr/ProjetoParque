Shader "Sprites/UnlitPM_Crossfade"
{
    Properties { _MainTex("Texture",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _Weight("Weight",Range(0,1))=1 }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off ZWrite Off ZTest LEqual
        Blend One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST, _Tint; float _Weight;
            struct A{float4 pos:POSITION; float2 uv:TEXCOORD0; float4 col:COLOR;};
            struct V{float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR;};
            V vert(A v){V o; o.pos=TransformObjectToHClip(v.pos.xyz); o.uv=TRANSFORM_TEX(v.uv,_MainTex); o.col=v.col*_Tint; return o;}
            float4 frag(V i):SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                float a = saturate(c.a * _Weight * i.col.a);
                float3 rgb = c.rgb * i.col.rgb * a;
                return float4(rgb,a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
