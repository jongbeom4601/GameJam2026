Shader "GameJam/RewindScreenFilter"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0
        _CyanColor ("Cyan", Color) = (0.0, 1.0, 0.9, 1.0)
        _MagentaColor ("Magenta", Color) = (1.0, 0.0, 0.75, 1.0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex FilterVert
            #pragma fragment FilterFrag
            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            float _Intensity;
            fixed4 _CyanColor;
            fixed4 _MagentaColor;

            VertexToFragment FilterVert(AppData input)
            {
                VertexToFragment output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                return output;
            }

            fixed4 FilterFrag(VertexToFragment input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 centered = uv - 0.5;
                float sideFade = smoothstep(0.14, 0.47, abs(centered.x));
                sideFade = pow(sideFade, 1.35);

                float scanline = 0.5 + 0.5 * sin(uv.y * 900.0 + _Time.y * 38.0);
                float colorWave = 0.5 + 0.5 * sin(
                    uv.y * 11.0 - uv.x * 7.0 + _Time.y * 4.5);
                float speedWave = 0.5 + 0.5 * sin(
                    (uv.x + uv.y * 0.32 + _Time.y * 1.8) * 52.0);
                float speedStreak = pow(speedWave, 12.0);

                fixed3 filterColor = lerp(
                    _CyanColor.rgb,
                    _MagentaColor.rgb,
                    colorWave * 0.65);
                float pulse = 0.88 + 0.12 * sin(_Time.y * 15.0);
                float alpha = (0.42 + scanline * 0.035 +
                    speedStreak * 0.13) * sideFade * _Intensity * pulse;

                return fixed4(filterColor, saturate(alpha));
            }
            ENDCG
        }
    }
}
