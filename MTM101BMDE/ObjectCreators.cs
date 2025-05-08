﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Net;
//BepInEx stuff
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using BepInEx.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace MTM101BaldAPI
{
	public static partial class ObjectCreators
	{
		public static StandardDoorMats CreateDoorDataObject(string name, Texture2D openTex, Texture2D closeTex)
		{
			StandardDoorMats template = MTM101BaldiDevAPI.AssetMan.Get<StandardDoorMats>("DoorTemplate");
			StandardDoorMats mat = ScriptableObject.CreateInstance<StandardDoorMats>();
            mat.open = new Material(template.open);
            mat.open.SetMainTexture(openTex);
            mat.shut = new Material(template.shut);
            mat.shut.SetMainTexture(closeTex);
			mat.name = name;


            return mat;

        }

		public static WindowObject CreateWindowObject(string name, Texture2D texture, Texture2D brokenTexture, Texture2D mask = null)
		{
			WindowObject obj = ScriptableObject.CreateInstance<WindowObject>();
			WindowObject template = MTM101BaldiDevAPI.AssetMan.Get<WindowObject>("WindowTemplate");
			obj.name = name;
			if (mask != null)
			{
				Material maskMat = new Material(template.mask);
				maskMat.SetMaskTexture(mask);
				obj.mask = maskMat;
			}
			else
			{
				obj.mask = template.mask;
			}
			Material standMat = new Material(template.overlay.First());
			standMat.SetMainTexture(texture);
			obj.overlay = new Material[] { standMat, standMat };
            Material BrokeMat = new Material(template.open.First());
            BrokeMat.SetMainTexture(brokenTexture);
            obj.open = new Material[] { BrokeMat, BrokeMat };
			obj.windowPre = template.windowPre;

            return obj;
		}

        public static SoundObject CreateSoundObject(AudioClip clip, string subtitle, SoundType type, Color color, float sublength = -1f)
		{
			SoundObject obj = ScriptableObject.CreateInstance<SoundObject>();
			obj.soundClip = clip;
			if (sublength == 0f)
			{
				obj.subtitle = false;
			}
			obj.subDuration = sublength == -1 ? clip.length + 1f : sublength;
			obj.soundType = type;
			obj.soundKey = subtitle;
			obj.color = color;
			obj.name = subtitle;
			return obj;

		}

        public static PosterObject CreatePosterObject(Texture2D postertex, PosterTextData[] text)
        {
            PosterObject obj = ScriptableObject.CreateInstance<PosterObject>();
            obj.baseTexture = postertex;
            obj.textData = text;
            obj.name = postertex.name + "Poster";

            return obj;
        }

		public static Material SpriteMaterial => MTM101BaldiDevAPI.AssetMan.Get<Material>("SpriteStandard_Billboard");

		/// <summary>
		/// Create a PosterObject in the style of a typical character poster.
		/// </summary>
		/// <param name="texture"></param>
		/// <param name="nameKey">The localization key that will show up as the character's name.</param>
		/// <param name="descKey">The localization key that will show up as the character's description.</param>
		/// <returns></returns>
		public static PosterObject CreateCharacterPoster(Texture2D texture, string nameKey, string descKey)
		{
			PosterObject obj = ScriptableObject.Instantiate<PosterObject>(MTM101BaldiDevAPI.AssetMan.Get<PosterObject>("CharacterPosterTemplate"));
			obj.name = nameKey + "Poster";
			obj.baseTexture = texture;
			obj.textData[0].textKey = nameKey;
            obj.textData[1].textKey = descKey;
            return obj;
		}

        public static PosterObject CreatePosterObject(Texture2D[] postertexs)
        {
			if (postertexs.Length == 0) throw new ArgumentNullException();
            PosterObject obj = ScriptableObject.CreateInstance<PosterObject>();
			obj.textData = new PosterTextData[0];
			if (postertexs.Length == 1)
			{
                obj.name = postertexs[0].name + "Poster";
				obj.baseTexture = postertexs.First();
            }
			else
			{
				List<PosterObject> otherPosters = new List<PosterObject>();
				for (int i = 0; i < postertexs.Length; i++)
				{
					otherPosters.Add(CreatePosterObject(postertexs[i],new PosterTextData[0]));
				}
				obj.multiPosterArray = otherPosters.ToArray();
                obj.name = postertexs[0].name + "PosterChain";
            }

            return obj;
        }
    }
}
