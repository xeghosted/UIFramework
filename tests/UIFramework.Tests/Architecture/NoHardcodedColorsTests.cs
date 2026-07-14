using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UIFramework.Controls;
using Xunit;

namespace UIFramework.Tests.Architecture
{
    public class NoHardcodedColorsTests
    {
        /// <summary>
        /// Color.Empty und Color.Transparent sind keine Gestaltungsentscheidung,
        /// sondern bedeuten "keine Farbe" bzw. "nicht füllen".
        /// </summary>
        private static readonly HashSet<string> Allowed = new HashSet<string>
        {
            "get_Empty",
            "get_Transparent"
        };

        [Fact]
        public void The_controls_assembly_contains_no_colour_of_its_own()
        {
            string assemblyPath = typeof(SkinButton).Assembly.Location;
            var offenders = new List<string>();

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                foreach (var type in assembly.MainModule.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody) continue;

                        foreach (var instruction in method.Body.Instructions)
                        {
                            var reference = instruction.Operand as MethodReference;
                            if (reference == null) continue;

                            // Nur statische Zugriffe: Color.Red, Color.FromArgb(...),
                            // SystemColors.Control. Instanzzugriffe wie farbe.R sind
                            // legitim — sie lesen eine Farbe, die aus dem Skin stammt.
                            if (reference.HasThis) continue;

                            string declaring = reference.DeclaringType.FullName;
                            if (declaring != "System.Drawing.Color" &&
                                declaring != "System.Drawing.SystemColors" &&
                                declaring != "System.Drawing.Brushes" &&
                                declaring != "System.Drawing.Pens")
                                continue;

                            if (Allowed.Contains(reference.Name)) continue;

                            offenders.Add(type.FullName + "." + method.Name +
                                          " → " + declaring + "." + reference.Name);
                        }
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "Farben gehören ausschließlich in die Skin-Klassen. Gefunden in UIFramework.Controls:\n  " +
                string.Join("\n  ", offenders.Distinct()));
        }

        [Fact]
        public void The_detector_actually_detects_something()
        {
            // Ein Wächter, der nie anschlägt, ist wertlos. Dieser Test prüft den
            // Wächter selbst gegen eine Assembly, die garantiert Farben enthält:
            // UIFramework.Core, wo LightSkin und DarkSkin leben.
            string corePath = typeof(UIFramework.Core.Skinning.Skins.LightSkin).Assembly.Location;
            var found = new List<string>();

            using (var assembly = AssemblyDefinition.ReadAssembly(corePath))
            {
                foreach (var type in assembly.MainModule.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody) continue;

                        foreach (var instruction in method.Body.Instructions)
                        {
                            var reference = instruction.Operand as MethodReference;
                            if (reference == null) continue;
                            if (reference.HasThis) continue;
                            if (reference.DeclaringType.FullName != "System.Drawing.Color") continue;
                            if (Allowed.Contains(reference.Name)) continue;

                            found.Add(reference.Name);
                        }
                    }
                }
            }

            Assert.NotEmpty(found);
        }
    }
}
