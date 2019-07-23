using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using alphaShot;
using BepInEx.Logging;
using Illusion.Extensions;
using Studio;
using UnityEngine;
using Logger = BepInEx.Logger;
using Object = UnityEngine.Object;

namespace KK_QuickAccessBox
{
	internal static class ThumbnailGenerator
	{
		public static IEnumerator MakeThumbnail(IEnumerable<ItemInfo> itemList, string thumbnailPath, string outputDirectory)
		{
			if (itemList == null) throw new ArgumentNullException(nameof(itemList));
			if (thumbnailPath == null) throw new ArgumentNullException(nameof(thumbnailPath));
			if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));
			if (!File.Exists(thumbnailPath)) throw new FileNotFoundException("Thumbnail image was not found in " + thumbnailPath);
			if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException("Output directory was not found: " + thumbnailPath);

			RemoveAllItems();

			// Spawn new image board > rectangle
			Singleton<Studio.Studio>.Instance.AddItem(1, 38, 671);
			yield return null;

			var root = GameObject.Find("CommonSpace").transform;

			// Setup thumbnail pane
			var origPlane = root.Find("p_koi_stu_plane00_00");
			foreach (var component in origPlane.GetComponents<MonoBehaviour>())
			{
				// Needed to prevent kkpe from crashing the Object.Instantiate call below
				if (component.GetType().Name == "PoseController")
					Object.DestroyImmediate(component);
			}
			var copyPlane = Object.Instantiate(origPlane);
			copyPlane.gameObject.SetActive(true);

			RemoveAllItems();

			// Load the thumbnail image
			var thumbBack = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			thumbBack.LoadImage(File.ReadAllBytes(thumbnailPath));
			var mat = copyPlane.GetComponent<MeshRenderer>().material;
			mat.mainTexture = thumbBack;

			var camera = Camera.main;
			var cameraControl = camera.GetComponent<Studio.CameraControl>();
			var alphaShot = camera.GetComponent<AlphaShot2>();

			var no = 1;
			foreach (var itemInfo in itemList.Shuffle().Take(10)) // todo remove
			{
				yield return null;
				yield return null;

				itemInfo.AddItem();

				var targets = root.Children();
				var b = CalculateBounds(targets);

				if (b != null)
				{
					var bounds = b.Value;

					cameraControl.enabled = false;

					var objectSize = bounds.size.magnitude;
					var targetObj = targets.First();
					// Use isometric angle since it has best chance of showing the object in an usable way
					// Move the camera a long way away from the objects to avoid clipping
					camera.transform.position = (bounds.center - targetObj.forward * objectSize - targetObj.right * objectSize + targetObj.up * objectSize) * 8;
					camera.transform.LookAt(bounds.center);
					camera.orthographic = true;
					const float objectSizeAdjust = 0.5f;
					camera.orthographicSize = objectSize * objectSizeAdjust;

					// Move the thumbnail pane in the opposite way from the camera and scale/align it so that it fills the camera view behind the item
					copyPlane.position = camera.transform.position + camera.transform.forward * (camera.transform.position - bounds.center).magnitude * 2;
					copyPlane.LookAt(camera.transform);
					copyPlane.localScale = new Vector3(bounds.size.magnitude / (16 / 9f) * 1.325F, bounds.size.magnitude * 1.325F, 1) * objectSizeAdjust;

					var result = alphaShot.Capture(100, 100, 1, true); //todo 2

					//todo use hashcode instead so it can be read
					File.WriteAllBytes(Path.Combine(outputDirectory, no++ + ".png"), result);
				}
				else
				{
					Logger.Log(LogLevel.Message, "No renderers to take capture of");
				}

				RemoveAllItems();
			}

			cameraControl.enabled = true;
			camera.orthographic = false;

			Object.Destroy(mat.mainTexture);
			Object.Destroy(copyPlane.gameObject);
		}

		private static Bounds? CalculateBounds(IEnumerable<Transform> targets)
		{
			Bounds? b = null;
			foreach (var renderer in targets.SelectMany(x => x.GetComponentsInChildren<Renderer>()))
			{
				if (b == null)
					b = renderer.bounds;
				else
					b.Value.Encapsulate(renderer.bounds);
			}

			return b;
		}

		private static void RemoveAllItems()
		{
			Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode = null;
			foreach (var treeNodeObject in Object.FindObjectsOfType<TreeNodeObject>().Where(x => x != null))
				Singleton<Studio.Studio>.Instance.treeNodeCtrl.DeleteNode(treeNodeObject);
		}
	}
}
