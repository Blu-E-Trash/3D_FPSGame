// 감시 카메라 화면 셰이더 (로그인 화면 RawImage용)
// 흑백 → 녹색, 대비/밝기, 볼록 렌즈 왜곡, 가장자리 색 번짐, 가장자리 어둡게
Shader "UI/CCTVFeed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mono ("Mono Color", Color) = (0.62, 1, 0.68, 1)
        _Contrast ("Contrast", Range(0.5, 2.5)) = 1.35
        _Brightness ("Brightness", Range(0, 2)) = 1.15
        _Distortion ("Barrel Distortion", Range(0, 0.5)) = 0.07
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.004
        _Vignette ("Vignette", Range(0, 3)) = 1.1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Mono;
            float _Contrast, _Brightness, _Distortion, _Aberration, _Vignette;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // 적외선 카메라처럼 파란 물체도 어둡게 묻히지 않도록, 평균과 가장 밝은 채널을 섞어 밝기를 정한다.
            float Lum(float3 c)
            {
                float avg = (c.r + c.g + c.b) / 3.0;
                float mx = max(c.r, max(c.g, c.b));
                return lerp(avg, mx, 0.5);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 볼록 렌즈: 가장자리로 갈수록 바깥쪽을 읽어서 화면이 둥글게 부푼다.
                float2 d = i.uv - 0.5;
                float r2 = dot(d, d);
                float2 uv = 0.5 + d * (1.0 + r2 * _Distortion);

                // 2. 가장자리 색 번짐: R/B를 조금씩 다른 위치에서 읽는다.
                float2 ab = d * _Aberration * 4.0;
                float lr = Lum(tex2D(_MainTex, uv + ab).rgb);
                float lg = Lum(tex2D(_MainTex, uv).rgb);
                float lb = Lum(tex2D(_MainTex, uv - ab).rgb);
                float3 mono = float3(lr, lg, lb);

                // 3. 대비/밝기 → 녹색
                mono = saturate((mono - 0.5) * _Contrast + 0.5) * _Brightness;
                float3 col = mono * _Mono.rgb;

                // 4. 가장자리 어둡게, 화면 밖은 검정
                float vig = saturate(1.0 - r2 * _Vignette);
                col *= vig * vig;
                float inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                col *= inside;

                return fixed4(col, 1.0) * i.color;
            }
            ENDCG
        }
    }
}
