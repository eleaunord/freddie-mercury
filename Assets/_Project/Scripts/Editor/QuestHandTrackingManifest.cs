#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEngine.XR.OpenXR;

namespace FreddieMercury.Editor
{
    // Declares hand tracking in the generated Android manifest.
    // XR Plugin Management calls every top level IAndroidManifestRequirementProvider
    // during the Android build. This is the same extension point MetaQuestFeature uses
    // internally for its own controller and eye tracking entries. Without these three
    // elements the Quest launcher assumes controllers are mandatory and keeps asking
    // the player to turn them on, even though the OpenXR hand interaction profile is
    // enabled and delivering hand poses. required="false" keeps controllers usable too.
    sealed class QuestHandTrackingManifest : IAndroidManifestRequirementProvider
    {
        public ManifestRequirement ProvideManifestRequirement()
        {
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type> { typeof(OpenXRLoader) },
                NewElements = new List<ManifestElement>
                {
                    new ManifestElement
                    {
                        ElementPath = new List<string> { "manifest", "uses-feature" },
                        Attributes = new Dictionary<string, string>
                        {
                            { "name", "oculus.software.handtracking" },
                            { "required", "false" },
                        },
                    },
                    new ManifestElement
                    {
                        ElementPath = new List<string> { "manifest", "uses-permission" },
                        Attributes = new Dictionary<string, string>
                        {
                            { "name", "com.oculus.permission.HAND_TRACKING" },
                        },
                    },
                    new ManifestElement
                    {
                        ElementPath = new List<string> { "manifest", "application", "meta-data" },
                        Attributes = new Dictionary<string, string>
                        {
                            { "name", "com.oculus.handtracking.version" },
                            { "value", "V2.0" },
                        },
                    },
                },
            };
        }
    }
}
#endif
