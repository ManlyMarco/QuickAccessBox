﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Bootstrap;
using Illusion.Extensions;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_QuickAccessBox.Thumbs
{
    internal static class ThumbnailGenerator
    {
        private static readonly HashSet<int> _shootFromFrontGroups = new HashSet<int> { 0, 9, 10, 501 };

        public static IEnumerator MakeThumbnail(IEnumerable<ItemInfo> itemList, string outputDirectory, bool manualMode, bool dark)
        {
            Texture2D thumbBackground;
            try
            {
                if (manualMode && !(Chainloader.PluginInfos.TryGetValue("KK_OrthographicCamera", out var plug) && plug.Metadata.Version >= new Version("1.1.1")))
                    throw new ArgumentException("Manual mode needs the OrthographicCamera plugin (at least v1.1.1) to work");

                if (itemList == null) throw new ArgumentNullException(nameof(itemList));
                if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));
                if (!Directory.Exists(outputDirectory)) throw new DirectoryNotFoundException("Output directory was not found: " + outputDirectory);

                thumbBackground = ResourceUtils.GetEmbeddedResource(dark ? "thumb_background_dark.png" : "thumb_background.png").LoadTexture();
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Error, "Failed to make thumbs: " + ex.Message);
                yield break;
            }

            RemoveAllItems();
            yield return null;

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

            var mat = copyPlane.GetComponent<MeshRenderer>().material;
            mat.mainTexture = thumbBackground;

            var camera = Camera.main;
            var cameraControl = camera.GetComponent<Studio.CameraControl>();

            camera.orthographic = true;
            cameraControl.enabled = false;

            byte[] RunCapture()
            {
                RenderSettings.ambientSkyColor = Color.white;
                RenderSettings.ambientEquatorColor = Color.white;
                RenderSettings.ambientGroundColor = Color.white;

                var cap = Screencap.ScreenshotManager.CaptureRender(64, 64, 1, Screencap.AlphaMode.rgAlpha);
                var cap2D = cap.ToTexture2D();
                var bytes = cap2D.EncodeToPNG();
                RenderTexture.ReleaseTemporary(cap);
                Object.Destroy(cap2D);
                return bytes;
            }

            var createdCount = 0;
            foreach (var itemInfo in itemList)
            {
                if (itemInfo.IsSFX) continue;

                var thumbPathNew = Path.Combine(outputDirectory, itemInfo.NewCacheId + ".png");
#pragma warning disable CS0612
                var thumbPathOld = Path.Combine(outputDirectory, itemInfo.OldCacheId + ".png");
#pragma warning restore CS0612
                if (File.Exists(thumbPathNew)) continue;
                if (File.Exists(thumbPathOld))
                {
                    // Convert thumbnails with old cache IDs to new cache IDs
                    Console.WriteLine($"Renaming [{thumbPathOld}] to [{thumbPathNew}]");
                    File.Move(thumbPathOld, thumbPathNew);
                    continue;
                }

                if (ThumbnailLoader.CustomThumbnailAvailable(itemInfo)) continue;

                yield return null;

                Console.WriteLine($"Spawning: FullName={itemInfo.FullName} FileName={itemInfo.FileName}");

                itemInfo.AddItem();
                createdCount++;

                yield return null;

                var targets = root.Children();
                var b = Utils.CalculateBounds(targets);

                if (b != null)
                {
                    var bounds = b.Value;

                    const float objectSizeAdjust = 0.5f;

                    var objectSize = bounds.size.magnitude;
                    var targetObj = targets.First();

                    if (_shootFromFrontGroups.Contains(itemInfo.GroupNo))
                    {
                        // Look at the object from front, better for flat effects
                        // Move the camera a long way away from the objects to avoid clipping
                        // todo anything better? Not reliable
                        camera.transform.position = (targetObj.position + targetObj.forward * objectSize) * 8;
                    }
                    else
                    {
                        // Use isometric angle since it has best chance of showing the object in an usable way
                        // Move the camera a long way away from the objects to avoid clipping
                        camera.transform.position = (bounds.center - targetObj.forward * objectSize - targetObj.right * objectSize + targetObj.up * objectSize) * 8;
                    }

                    camera.transform.LookAt(bounds.center);
                    camera.orthographicSize = objectSize * objectSizeAdjust;

                    void UpdateBackgroundPlane()
                    {
                        // Move the thumbnail pane in the opposite way from the camera and scale/align it so that it fills the camera view behind the item
                        copyPlane.position = camera.transform.position + camera.transform.forward * (camera.transform.position - bounds.center).magnitude * 2;
                        copyPlane.LookAt(camera.transform);
                        copyPlane.localScale = new Vector3(objectSize / (16 / 9f) * 1.325F, objectSize * 1.325F, 1) * objectSizeAdjust;
                    }

                    if (manualMode)
                    {
                        cameraControl.enabled = true;
                        yield return new WaitUntil(() =>
                        {
                            // Adjust for the new camera position
                            objectSize = camera.orthographicSize / objectSizeAdjust;
                            UpdateBackgroundPlane();
                            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKey(KeyCode.Escape);
                        });
                        cameraControl.enabled = false;
                    }

                    UpdateBackgroundPlane();
                    
                    var result = RunCapture();

                    try { File.WriteAllBytes(thumbPathNew, result); }
                    catch (SystemException ex) { QuickAccessBox.Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Error, "Failed to write thumbnail file - " + ex.Message); }
                }
                else
                {
                    QuickAccessBox.Logger.LogInfo("No renderers to take capture of - " + itemInfo);
                }

                yield return null;
                RemoveAllItems();

                if (createdCount % 400 == 0) Resources.UnloadUnusedAssets();

                if (Input.GetKey(KeyCode.Escape)) break;
            }

            cameraControl.enabled = true;
            camera.orthographic = false;

            Object.Destroy(thumbBackground);
            Object.Destroy(copyPlane.gameObject);

            QuickAccessBox.Logger.LogMessage("Finished taking thumbnails!");
        }

        private static void RemoveAllItems()
        {
            Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNode = null;
            foreach (var treeNodeObject in Object.FindObjectsOfType<TreeNodeObject>().Where(x => x != null))
                Singleton<Studio.Studio>.Instance.treeNodeCtrl.DeleteNode(treeNodeObject);

            foreach (Transform unofficialChild in GameObject.Find("CommonSpace").transform)
                Object.Destroy(unofficialChild.gameObject);
        }
    }
}
