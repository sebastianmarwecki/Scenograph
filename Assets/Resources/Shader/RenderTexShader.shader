// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TransparentRenderTexture" {
	Properties{
		_MainTex("Black (RGB)", 2D) = "black" {}
	}

		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Blend SrcAlpha SrcAlpha
		ZWrite off

		Pass{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

		struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	sampler2D _MainTex;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = v.texcoord;
		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		float4 black = tex2D(_MainTex, i.texcoord);
		return float4(black.r, black.g, black.b, 1);
	}
		ENDCG
	}
	}
}
