using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace OccaSoftware.TextureChannelPacker.Editor
{

	[Serializable]
	internal sealed class TextureData
	{
		public Texture2D texture = null;
		public TextureChannelSource textureChannelSource = TextureChannelSource.Red;
		public List<Color> colors = new List<Color>();
		public List<float> extractedColorDataFromChannel = new List<float>();
		public bool invert = false;
		public float clearValue = 0f;

		public void ExtractColorData(Vector2Int resolution)
		{
			extractedColorDataFromChannel.Clear();

			if (texture == null)
			{
				for (int i = 0; i < resolution.x * resolution.y; i++)
				{
					extractedColorDataFromChannel.Add(clearValue);
				}
			}
			else
			{
				colors = GetReadableTexture().GetPixels(0, 0, resolution.x, resolution.y, 0).ToList();
				foreach (Color color in colors)
				{
					extractedColorDataFromChannel.Add(ExtractColorChannelData(color));
				}
			}
		}


		private Texture2D GetReadableTexture()
		{
			if (texture.isReadable)
				return texture;


			RenderTexture temporaryRT = RenderTexture.GetTemporary(
					texture.width,
					texture.height,
					0,
					RenderTextureFormat.Default,
					RenderTextureReadWrite.Linear);


			RenderTexture currentActiveRT = RenderTexture.active;
			RenderTexture.active = temporaryRT;

			Graphics.Blit(texture, temporaryRT);

			Texture2D newTexture = new Texture2D(texture.width, texture.height);
			newTexture.ReadPixels(new Rect(0, 0, temporaryRT.width, temporaryRT.height), 0, 0);
			newTexture.Apply();

			RenderTexture.active = currentActiveRT;
			RenderTexture.ReleaseTemporary(temporaryRT);
			return newTexture;
		}

		private float ExtractColorChannelData(Color color)
		{
			float colorData = 0f;
			switch (textureChannelSource)
			{
				case TextureChannelSource.Red:
					colorData = color.r;
					break;
				case TextureChannelSource.Green:
					colorData = color.g;
					break;
				case TextureChannelSource.Blue:
					colorData = color.b;
					break;
				case TextureChannelSource.Alpha:
					colorData = color.a;
					break;
			}

			if (invert)
				colorData = 1.0f - colorData;

			return colorData;
		}

		public void Validate()
		{
			clearValue = Math.Clamp(clearValue, 0, 1);
		}
		public enum TextureChannelSource
		{
			Red,
			Green,
			Blue,
			Alpha
		}

		public TextureData()
		{

		}
	}
}