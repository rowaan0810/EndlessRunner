Shader "Morioh/Hybrid Painted Sky"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Painted lavender sky", 2D) = "white" {}
        _Tint ("Painting tint", Color) = (1,1,1,1)
        _HorizonColor ("Horizon / fog match", Color) = (.66,.59,.71,1)
        _Exposure ("Exposure", Range(.3,2)) = 1
        _PaintingHeight ("Painting height degrees", Range(20,90)) = 65
        _Rotation ("Painting rotation degrees", Range(0,360)) = 0
        _HorizonBlendHeight ("Horizon blend height", Range(.01,.3)) = .08
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Tint, _HorizonColor;
            float _Exposure, _PaintingHeight, _Rotation, _HorizonBlendHeight;
            struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(float4 vertex : POSITION) { v2f o; o.vertex = UnityObjectToClipPos(vertex); o.direction = vertex.xyz; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float u = atan2(d.x,d.z) / 6.2831853 + .5 + _Rotation / 360;
                // Mirror the horizontal artwork to remove any painted edge seam.
                float2 uv = float2(abs(frac(u) * 2 - 1), saturate(asin(clamp(d.y,-1,1)) * 57.29578 / _PaintingHeight));
                float3 painting = tex2D(_MainTex, uv).rgb * _Tint.rgb * _Exposure;
                return float4(lerp(_HorizonColor.rgb, painting, smoothstep(0,_HorizonBlendHeight,d.y)),1);
            }
            ENDCG
        }
    }
}
