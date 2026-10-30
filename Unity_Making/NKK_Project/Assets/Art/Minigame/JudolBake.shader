// 쥐돌깨기 쥐 그림 미리 그리기 (JudolRatArt, GL 로 RenderTexture 에 그림)
// 패스 0 = 덮어쓰기 (칸 바탕 · 오라: 색을 그대로 남겨 가장자리가 검게 번지지 않게) · 패스 1 = 보통 겹치기 (알파는 쌓임)
Shader "NKK/JudolBake"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 col : COLOR; };
        v2f vert (appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; o.col = v.color; return o; }
        fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.uv) * i.col; }
        ENDCG

        Pass
        {
            Blend One Zero
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}
