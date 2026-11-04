Shader "Morioh/Lavender Cloud Sky"
{
    Properties
    {
        _Zenith ("Lavender zenith", Color) = (.39,.36,.58,1)
        _Horizon ("Warm lavender horizon", Color) = (.66,.59,.71,1)
        _Ground ("Below horizon", Color) = (.38,.34,.46,1)
        _Clouds ("Original cloud panorama", 2D) = "black" {}
        _CloudTint ("Cloud tint", Color) = (1,1,1,1)
        _Rotation ("Cloud rotation degrees", Range(0,360)) = 0
        _Exposure ("Exposure", Range(.1,2)) = 1
        _GradientPower ("Gradient height", Range(.1,3)) = .65
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
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; };
            sampler2D _Clouds;
            float4 _Zenith, _Horizon, _Ground, _CloudTint;
            float _Rotation, _Exposure, _GradientPower;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.direction = v.vertex.xyz; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float2 uv = float2(atan2(d.x,d.z) / 6.2831853 + .5 + _Rotation / 360,
                                  asin(clamp(d.y,-1,1)) / 3.14159265 + .5);
                float3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(d.y), _GradientPower));
                sky = lerp(_Ground.rgb, sky, smoothstep(-.08,.02,d.y));
                float4 cloud = tex2D(_Clouds, uv);
                return float4(lerp(sky, cloud.rgb * _CloudTint.rgb, cloud.a * _CloudTint.a) * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
