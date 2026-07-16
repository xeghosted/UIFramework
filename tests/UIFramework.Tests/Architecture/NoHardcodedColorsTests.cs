using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UIFramework.Controls;
using UIFramework.Grid;
using Xunit;

namespace UIFramework.Tests.Architecture
{
    public class NoHardcodedColorsTests
    {
        /// <summary>
        /// Color.Empty und Color.Transparent sind keine Gestaltungsentscheidung,
        /// sondern bedeuten "keine Farbe" bzw. "nicht füllen". Die Erlaubnis ist an
        /// den deklarierenden Typ UND den Membernamen gebunden — nicht nur an den
        /// Namen —, sonst würden auch Brushes.Transparent oder Pens.Transparent
        /// durchrutschen, obwohl nur System.Drawing.Color davon ausgenommen ist.
        /// </summary>
        private static readonly HashSet<string> Allowed = new HashSet<string>
        {
            "System.Drawing.Color.get_Empty",
            "System.Drawing.Color.get_Transparent"
        };

        /// <summary>
        /// Typen, über die eine Farbe erzeugt oder bezogen werden kann. Neben den
        /// offensichtlichen (Color, SystemColors, Brushes, Pens) auch die weniger
        /// offensichtlichen Verstecke: ColorTranslator (FromHtml/FromOle/FromWin32)
        /// sowie SystemBrushes/SystemPens.
        /// </summary>
        private static readonly HashSet<string> WatchedDeclaringTypes = new HashSet<string>
        {
            "System.Drawing.Color",
            "System.Drawing.SystemColors",
            "System.Drawing.Brushes",
            "System.Drawing.Pens",
            "System.Drawing.ColorTranslator",
            "System.Drawing.SystemBrushes",
            "System.Drawing.SystemPens"
        };

        /// <summary>
        /// Die Assemblies, in denen kein Farbwert stehen darf. UIFramework.Core
        /// fehlt bewusst: Dort leben LightSkin und DarkSkin, die einzigen Stellen
        /// im Framework, die Farben enthalten dürfen.
        /// </summary>
        public static TheoryData<string, string> GuardedAssemblies()
        {
            return new TheoryData<string, string>
            {
                { "UIFramework.Controls", typeof(SkinButton).Assembly.Location },
                { "UIFramework.Grid", typeof(GridControl).Assembly.Location }
            };
        }

        [Theory]
        [MemberData(nameof(GuardedAssemblies))]
        public void A_guarded_assembly_contains_no_colour_of_its_own(string name, string assemblyPath)
        {
            var offenders = new List<string>();
            int typeCount;
            int methodsWithBody = 0;

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                var types = assembly.MainModule.GetTypes().ToList();
                typeCount = types.Count;

                foreach (var type in types)
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody) continue;
                        methodsWithBody++;

                        foreach (var instruction in method.Body.Instructions)
                        {
                            var reference = instruction.Operand as MethodReference;
                            if (reference == null) continue;

                            // Nur statische Zugriffe: Color.Red, Color.FromArgb(...),
                            // SystemColors.Control. Instanzzugriffe wie farbe.R sind
                            // legitim — sie lesen eine Farbe, die aus dem Skin stammt.
                            if (reference.HasThis) continue;

                            string declaring = reference.DeclaringType.FullName;
                            if (!WatchedDeclaringTypes.Contains(declaring)) continue;

                            if (Allowed.Contains(declaring + "." + reference.Name)) continue;

                            offenders.Add(type.FullName + "." + method.Name +
                                          " → " + declaring + "." + reference.Name);
                        }
                    }
                }
            }

            // Ein Wächter, der über eine leere oder unvollständig geladene Assembly
            // läuft, würde stillschweigend "keine Verstöße" melden. Das darf nicht
            // mit einer echten, sauberen Assembly verwechselt werden.
            Assert.True(typeCount > 0,
                "Die gescannte Assembly " + name + " enthält keine Typen — der Test wäre vakuos.");
            Assert.True(methodsWithBody > 0,
                "Die gescannte Assembly " + name + " enthält keine Methoden mit Rumpf — der Test wäre vakuos.");

            Assert.True(offenders.Count == 0,
                "Farben gehören ausschließlich in die Skin-Klassen. Gefunden in " + name + ":\n  " +
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
                            string declaring = reference.DeclaringType.FullName;
                            if (declaring != "System.Drawing.Color") continue;
                            if (Allowed.Contains(declaring + "." + reference.Name)) continue;

                            found.Add(reference.Name);
                        }
                    }
                }
            }

            Assert.NotEmpty(found);
        }
    }
}
