Shader "Mobile/Texture Only"
{
	Properties
	{
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Texture (RGB)", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags { "Queue"="Geometry" "IgnoreProjector"="True" }
		Cull Off
		Lighting Off
		Fog { Mode Off }
		
		Pass
		{
			SetTexture [_MainTex]
			{
				constantColor [_Color]
				combine texture * constant
			}
		}
	}
}