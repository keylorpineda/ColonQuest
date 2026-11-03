using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColonQuest.World.Tests
{
    /// <summary>
    /// Every text written into the generated scenes (the prompts and lines of the things and people, and the letters of
    /// the signs) has an English entry, so changing the language never leaves a sign or a talk in Spanish.
    /// </summary>
    [TestFixture]
    public sealed class SceneTextsTests
    {
        private static readonly string[] Scenes =
        {
            SceneNames.Town, SceneNames.Casa, SceneNames.Cuarto, SceneNames.Tienda, SceneNames.Banco, SceneNames.Agencia,
            SceneNames.Carcel, SceneNames.Hospital, SceneNames.HospitalArriba, SceneNames.Vecino, SceneNames.Finca,
            SceneNames.Inmobiliaria, SceneNames.Soda, SceneNames.Cooperativa
        };

        /// <summary>Only texts with words need a translation: prices, numbers and symbols read the same.</summary>
        private static bool NeedsTranslation(string text)
        {
            return !string.IsNullOrEmpty(text) && text.Any(char.IsLetter);
        }

        [Test]
        public void SceneTexts_AllHaveAnEnglishEntry()
        {
            var known = new HashSet<string>(Loc.KnownTexts);
            var missing = new List<string>();
            foreach (var name in Scenes)
            {
                string path = "Assets/Scenes/" + name + ".unity";
                if (!System.IO.File.Exists(path))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var station in root.GetComponentsInChildren<Station>(true))
                        {
                            Check(station.DisplayName, name + " / " + station.name + " (prompt)", known, missing);
                            // A real estate station keeps a listing id in its lines; the player never reads it.
                            if (station.Type == StationType.RealEstate)
                            {
                                continue;
                            }

                            foreach (var line in station.Lines ?? new string[0])
                            {
                                Check(line, name + " / " + station.name + " (line)", known, missing);
                            }
                        }

                        foreach (var sign in root.GetComponentsInChildren<LocalizedMesh>(true))
                        {
                            Check(sign.Spanish, name + " / sign", known, missing);
                        }
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            Assert.IsEmpty(missing, "texts with no English entry:\n" + string.Join("\n", missing.Distinct()));
        }

        private static void Check(string text, string where, HashSet<string> known, List<string> missing)
        {
            if (NeedsTranslation(text) && !known.Contains(text))
            {
                missing.Add(where + ": " + text);
            }
        }
    }
}
