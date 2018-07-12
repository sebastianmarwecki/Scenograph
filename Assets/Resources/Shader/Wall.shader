Shader "VirtualSpace/Wall" {
	Properties{
		_Tex1("Texture 1", 2D) = "black" {}
		_MinDistance1("Min Distance 1", Float) = 100
		_MaxDistance1("Max Distance 1", Float) = 1000
			_MinColor1("Color in Minimal 1", Color) = (1, 1, 1, 1)
			_MaxColor1("Color in Maxmal 1", Color) = (0, 0, 0, 0)

			_Tex2("Texture 2 (optional)", 2D) = "black" {}
		_MinDistance2("Min Distance 2", Float) = 100
		_MaxDistance2("Max Distance 2", Float) = 1000
			_MinColor2("Color in Minimal 2", Color) = (1, 1, 1, 1)
			_MaxColor2("Color in Maxmal 2", Color) = (0, 0, 0, 0)

	//		_Tex3("Texture 3 (optional)", 2D) = "black" {}
	//	_MinDistance3("Min Distance 3", Float) = 100
	//	_MaxDistance3("Max Distance 3", Float) = 1000
	//		_MinColor3("Color in Minimal 3", Color) = (1, 1, 1, 1)
	//		_MaxColor3("Color in Maxmal 3", Color) = (0, 0, 0, 0)

	}
		SubShader{
		Tags{ "Queue" = "Transparent" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
		LOD 200
		ZWrite Off
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert alpha

		sampler2D _Tex1;
		sampler2D _Tex2;
	//	sampler2D _Tex3;

	struct Input {
		float2 uv_Tex1;
		float2 uv_Tex2;
		//float2 uv_Tex3;
		float3 worldPos;
	};

	float _MaxDistance1;
	float _MinDistance1;
	float _MaxDistance2;
	float _MinDistance2;
//	float _MaxDistance3;
	//float _MinDistance3;
	half4 _MinColor1;
	half4 _MaxColor1;
	half4 _MinColor2;
	half4 _MaxColor2;
//	half4 _MinColor3;
//	half4 _MaxColor3;

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c1 = tex2D(_Tex1, IN.uv_Tex1);
		half4 c2 = tex2D(_Tex2, IN.uv_Tex2);
	//	half4 c3 = tex2D(_Tex3, IN.uv_Tex3);
		float dist = distance(_WorldSpaceCameraPos, IN.worldPos);
		half weight1 = saturate((dist - _MinDistance1) / (_MaxDistance1 - _MinDistance1));
		half weight2 = saturate((dist - _MinDistance2) / (_MaxDistance2 - _MinDistance2));
	//	half weight3 = saturate((dist - _MinDistance3) / (_MaxDistance3 - _MinDistance3));
		half4 distanceColor1 = lerp(_MinColor1, _MaxColor1, weight1);
		half4 distanceColor2 = lerp(_MinColor2, _MaxColor2, weight2);
	//	half4 distanceColor3 = lerp(_MinColor3, _MaxColor3, weight3);

		//half maxAlb = max(c1.a * distanceColor1.rgb, c2.a * distanceColor2.rgb);
		//half maxAlb2 = max(maxAlb, c3.a * distanceColor3.rgb);
		o.Albedo = c1.a * distanceColor1.rgb + c2.a * distanceColor2.rgb;// +c3.a * distanceColor3.rgb;
		//o.Alpha = 1.0;// c.a * distanceColor1.a + c2.a* distanceColor2.a;// +c3.a* distanceColor3.a;//
		o.Alpha = c1.a  * distanceColor1.a + c2.a  * distanceColor2.a;// +c3.a  * distanceColor3.a;
		///o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	}
	ENDCG
	}
		FallBack "Diffuse"
}