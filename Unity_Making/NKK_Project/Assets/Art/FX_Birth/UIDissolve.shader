// UI 디졸브 (번식 탄생 연출 하트: 녹듯이 사라짐). UI/Default 와 같은 구조 + 노이즈로 잘라냄 (노이즈는 셰이더 안에서 계산, 텍스처 없음)
// _Cut 0 = 다 보임 → 1 = 다 사라짐. 잘리는 가장자리는 _EdgeColor 로 빛남 (폭 _Edge)
Shader "NKK/UIDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cut ("사라진 정도 (0~1)", Range(0, 1)) = 0
        _Edge ("빛나는 가장자리 폭", Range(0, 0.3)) = 0.08
        _EdgeColor ("가장자리 색", Color) = (1, 0.95, 0.75, 1)
        _NoiseScale ("노이즈 크기 (클수록 잘게)", Float) = 7

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color, _EdgeColor, _TextureSampleAdd;
            float4 _ClipRect;
            float _Cut, _Edge, _NoiseScale;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash (float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise (float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }
            float fbm (float2 p) { return vnoise(p) * 0.55 + vnoise(p * 2.1 + 3.7) * 0.3 + vnoise(p * 4.3 + 9.1) * 0.15; }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                // 노이즈 0~1 을 가장자리 폭만큼 넓혀서 _Cut 0 이면 다 보이고 1 이면 다 사라지게
                float n = fbm(i.uv * _NoiseScale);
                float k = _Cut * (1 + _Edge);
                clip(n - k + 0.0001);
                float e = 1 - saturate((n - k) / max(_Edge, 0.0001));      // 1 = 막 잘리는 가장자리
                if (_Cut > 0.001) c.rgb = lerp(c.rgb, _EdgeColor.rgb, e * _EdgeColor.a);
                return c;
            }
            ENDCG
        }
    }
}
