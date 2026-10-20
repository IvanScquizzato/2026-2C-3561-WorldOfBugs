using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using TGC.MonoGame.TP.Bush;
using TGC.MonoGame.TP.Car;
using TGC.MonoGame.TP.Debri;
using TGC.MonoGame.TP.Fences;
using TGC.MonoGame.TP.Ground;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Monument;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.RuinHouse;
using TGC.MonoGame.TP.Trees;
using TGC.MonoGame.TP.Walls;

namespace TGC.MonoGame.TP.Editor
{
    /// <summary>
    /// Archivo de escenario independiente del editor. Guarda tipo concreto, ruta del
    /// modelo y transformaciones. Los tanques, misiles y el terreno no son parte del nivel.
    /// </summary>
    public sealed class SceneRepository
    {
        public sealed class SceneFile
        {
            public int Version { get; set; } = 1;
            // false solamente en la plantilla inicial: importa el escenario antiguo una vez.
            public bool Initialized { get; set; } = false;
            public List<SceneObject> Objects { get; set; } = new();
        }

        public sealed class SceneObject
        {
            public string Id { get; set; } = "";
            public string Type { get; set; } = "";
            public string Model { get; set; } = "";
            public float[] Position { get; set; } = Array.Empty<float>();
            // Quaternion: x, y, z, w (evita ambigüedades con Euler).
            public float[] Rotation { get; set; } = Array.Empty<float>();
            // Vector3, incluso si el editor trabaja con escala uniforme.
            public float[] Scale { get; set; } = Array.Empty<float>();
        }

        public sealed class LegacyTransform
        {
            public string Id { get; set; } = "";
            public string? SourceId { get; set; }
            public float[] Position { get; set; } = Array.Empty<float>();
            public float[]? Rotation { get; set; }
            public float? RotationY { get; set; }
            public float? Scale { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public string FilePath { get; }

        public SceneRepository()
        {
            // Durante desarrollo usa Scenes/scene.json junto al .csproj, para poder
            // versionarlo en Git. Publicado, usa Scenes/ al lado del ejecutable.
            string? root = FindProjectRoot(AppContext.BaseDirectory) ??
                           FindProjectRoot(Environment.CurrentDirectory);
            FilePath = Path.Combine(root ?? AppContext.BaseDirectory, "Scenes", "scene.json");
        }

        private static string? FindProjectRoot(string start)
        {
            DirectoryInfo? current = new DirectoryInfo(Path.GetFullPath(start));
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "TGC.MonoGame.TP.csproj")))
                    return current.FullName;
                current = current.Parent;
            }
            return null;
        }

        public SceneFile Load()
        {
            if (!File.Exists(FilePath)) return new SceneFile();

            SceneFile scene = JsonSerializer.Deserialize<SceneFile>(File.ReadAllText(FilePath), JsonOptions)
                ?? throw new InvalidDataException("El JSON del escenario está vacío.");
            if (scene.Version != 1)
                throw new InvalidDataException($"Versión de escena no soportada: {scene.Version}.");
            if (scene.Objects == null)
                throw new InvalidDataException("La propiedad Objects debe ser un array.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (SceneObject obj in scene.Objects)
            {
                if (string.IsNullOrWhiteSpace(obj.Id) || !ids.Add(obj.Id))
                    throw new InvalidDataException($"ID de escena vacío o duplicado: '{obj.Id}'.");
                if (string.IsNullOrWhiteSpace(obj.Model) || string.IsNullOrWhiteSpace(obj.Type))
                    throw new InvalidDataException($"Tipo o modelo inválido para '{obj.Id}'.");
                if (obj.Position?.Length != 3 || obj.Rotation?.Length != 4 || obj.Scale?.Length != 3)
                    throw new InvalidDataException($"Transformación inválida en '{obj.Id}'.");
                if (!obj.Position.All(float.IsFinite) || !obj.Rotation.All(float.IsFinite) ||
                    !obj.Scale.All(v => float.IsFinite(v) && v > 0f))
                    throw new InvalidDataException($"Valores numéricos inválidos en '{obj.Id}'.");
                if (new Quaternion(obj.Rotation[0], obj.Rotation[1], obj.Rotation[2], obj.Rotation[3]).LengthSquared() < 0.00001f)
                    throw new InvalidDataException($"Rotación inválida en '{obj.Id}'.");
            }
            return scene;
        }

        public void Save(IEnumerable<KeyValuePair<string, ModelInScene>> objects)
        {
            var scene = new SceneFile
            {
                Initialized = true,
                Objects = objects.Select(kv => Capture(kv.Key, kv.Value))
                    .OrderBy(obj => obj.Id, StringComparer.Ordinal).ToList()
            };

            string folder = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(folder);
            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(scene, JsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }

        public static SceneObject Capture(string id, ModelInScene model)
        {
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(model._rotation);
            return new SceneObject
            {
                Id = id,
                Type = model.GetType().Name,
                Model = model._modelPath,
                Position = new[] { model._position.X, model._position.Y, model._position.Z },
                Rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W },
                Scale = new[] { model._scale.X, model._scale.Y, model._scale.Z }
            };
        }

        public static Vector3 GetPosition(SceneObject data) =>
            new(data.Position[0], data.Position[1], data.Position[2]);

        public static Matrix GetRotation(SceneObject data)
        {
            Quaternion q = new(data.Rotation[0], data.Rotation[1], data.Rotation[2], data.Rotation[3]);
            q.Normalize();
            return Matrix.CreateFromQuaternion(q);
        }

        public static Vector3 GetScale(SceneObject data) =>
            new(data.Scale[0], data.Scale[1], data.Scale[2]);

        public static ModelInScene Create(SceneObject data, ContentManager content,
            IModelInSceneRenderer renderer)
        {
            Vector3 position = GetPosition(data);
            Matrix rotation = GetRotation(data);
            Vector3 scale = GetScale(data);
            string path = data.Model;

            return data.Type switch
            {
                nameof(Tree) => new Tree(content, path, position, rotation, scale, renderer),
                nameof(RuinHouse1) => new RuinHouse1(content, path, position, rotation, scale, renderer),
                nameof(Fence) => new Fence(content, path, position, rotation, scale, renderer),
                nameof(Debris) => new Debris(content, path, position, rotation, scale, renderer),
                nameof(AbandonedCar) => new AbandonedCar(content, path, position, rotation, scale, renderer),
                nameof(Obelisc) => new Obelisc(content, path, position, rotation, scale, renderer),
                nameof(Grass) => new Grass(content, path, position, rotation, scale, renderer),
                nameof(DeadBush) => new DeadBush(content, path, position, rotation, scale, renderer),
                nameof(Wall) => new Wall(content, path, position, rotation, scale, renderer),
                _ => throw new InvalidDataException($"Tipo de modelo no soportado: '{data.Type}' (ID: {data.Id}).")
            };
        }

        /// <summary>Lee la antigua carpeta Documentos/WorldOfBugs solo al migrar.</summary>
        public IReadOnlyList<LegacyTransform> ReadLegacy()
        {
            string oldFile = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "WorldOfBugs", "scene.json");
            if (!File.Exists(oldFile)) return Array.Empty<LegacyTransform>();
            List<LegacyTransform>? result = JsonSerializer.Deserialize<List<LegacyTransform>>(
                File.ReadAllText(oldFile), JsonOptions);
            return result ?? (IReadOnlyList<LegacyTransform>)Array.Empty<LegacyTransform>();
        }

        public static Matrix LegacyRotation(LegacyTransform data)
        {
            if (data.Rotation is { Length: 4 } q)
            {
                Quaternion rotation = new(q[0], q[1], q[2], q[3]);
                if (rotation.LengthSquared() > 0.00001f)
                {
                    rotation.Normalize();
                    return Matrix.CreateFromQuaternion(rotation);
                }
            }
            return data.RotationY.HasValue
                ? Matrix.CreateRotationY(data.RotationY.Value)
                : Matrix.Identity;
        }
    }
}
