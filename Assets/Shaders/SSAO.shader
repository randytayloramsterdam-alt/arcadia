Shader "Hidden/SSAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: SSAO calculation
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;

            float _SampleRadius;
            float _Intensity;
            float _Bias;
            int _SampleCount;
            float4 _SampleKernel[16];
            float4x4 _FrustumCorners;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Reconstruct view direction from frustum corner
                float4 corner = lerp(
                    lerp(_FrustumCorners[0], _FrustumCorners[1], v.uv.x),
                    lerp(_FrustumCorners[3], _FrustumCorners[2], v.uv.x),
                    v.uv.y
                );
                o.viewDir = corner.xyz / corner.w;
                return o;
            }

            float3 GetViewPos(float2 uv, float3 viewDir)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float linearDepth = LinearEyeDepth(depth);
                return viewDir * linearDepth;
            }

            float DoSSAO(float2 uv, float3 viewPos, float3 viewDir)
            {
                float occlusion = 0.0;
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));

                // Skip sky / far pixels
                if (depth > 50.0) return 0.0;

                // Generate random rotation from noise
                float3 randomVec = float3(frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453),
                                          frac(sin(dot(uv, float2(39.346, 21.731))) * 38219.2276),
                                          0.0);
                randomVec = normalize(randomVec);

                float3 tangent = normalize(randomVec - viewDir * dot(randomVec, viewDir));
                float3 bitangent = cross(viewDir, tangent);
                float3x3 TBN = float3x3(tangent, bitangent, viewDir);

                for (int i = 0; i < 8; i++)
                {
                    float3 sampleDir = mul(_SampleKernel[i].xyz, TBN);
                    sampleDir = sampleDir * _SampleRadius;

                    float4 offset = float4(sampleDir, 0.0);
                    float4 samplePos = float4(viewPos + offset.xyz, 1.0);

                    float4 clipPos = mul(unity_CameraProjection, samplePos);
                    float2 sampleUV = (clipPos.xy / clipPos.w) * 0.5 + 0.5;

                    float sampleDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampleUV));
                    float rangeCheck = smoothstep(0.0, 1.0, _SampleRadius / abs(viewPos.z - sampleDepth));

                    if (sampleDepth < (viewPos.z - _Bias))
                        occlusion += rangeCheck;
                }

                return saturate(1.0 - (occlusion / 8.0) * _Intensity);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewPos = GetViewPos(i.uv, i.viewDir);
                float3 viewDir = normalize(-viewPos);
                float ao = DoSSAO(i.uv, viewPos, viewDir);

                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb *= ao;
                return col;
            }
            ENDCG
        }

        // Pass 1: 5x5 bilateral blur (horizontal)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));

                float4 sum = 0.0;
                float totalWeight = 0.0;

                float weights[5] = { 0.16, 0.28, 0.12, 0.08, 0.02 };
                float offsets[5] = { 0.0, 1.0, 2.0, 3.0, 4.0 };

                for (int d = 0; d < 5; d++)
                {
                    float2 offset0 = float2(offsets[d] * texelSize.x * 2.0, 0);
                    float2 offset1 = float2(-offsets[d] * texelSize.x * 2.0, 0);

                    float sampleDepth0 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv + offset0));
                    float sampleDepth1 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv + offset1));

                    float w0 = weights[d] / (1.0 + abs(depth - sampleDepth0) * 10.0);
                    float w1 = weights[d] / (1.0 + abs(depth - sampleDepth1) * 10.0);

                    sum += tex2D(_MainTex, i.uv + offset0) * w0;
                    sum += tex2D(_MainTex, i.uv + offset1) * w1;
                    totalWeight += w0 + w1;
                }

                return sum / totalWeight;
            }
            ENDCG
        }
    }
}
