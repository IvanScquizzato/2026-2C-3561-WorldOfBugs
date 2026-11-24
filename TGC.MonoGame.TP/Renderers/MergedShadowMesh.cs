using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Renderers;

/// <summary>
/// Versión unificada de un Model para la pasada de sombras: todas sus partes
/// (con las transformaciones de los huesos ya aplicadas) quedan en pocos
/// vertex/index buffers. Para modelos opacos se ignoran las texturas y queda
/// un único grupo; para cutout se agrupa por textura porque el shader la
/// necesita para el clip del alpha.
///
/// El perfil Reach solo admite índices de 16 bits, así que cada grupo se parte
/// en lotes de hasta 65535 vértices.
/// </summary>
public sealed class MergedShadowMesh : IDisposable
{
    private const int MaxVerticesPerBatch = ushort.MaxValue;

    public sealed class Batch
    {
        public VertexBuffer Vertices;
        public IndexBuffer Indices;
        public int PrimitiveCount;
        public Texture2D Texture;
    }

    public List<Batch> Batches { get; } = new();

    public static MergedShadowMesh Build(
        GraphicsDevice graphicsDevice,
        Model model,
        List<Texture2D> textures,
        bool groupByTexture)
    {
        var bones = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(bones);

        // Datos crudos de cada buffer, leídos una sola vez (varias partes
        // suelen compartir el mismo vertex/index buffer).
        var vertexCache = new Dictionary<VertexBuffer, (Vector3[] Positions, Vector2[] UVs)>();
        var indexCache = new Dictionary<IndexBuffer, int[]>();

        // Partes agrupadas por textura (null = grupo único para opacos).
        var groups = new Dictionary<Texture2D, List<(ModelMeshPart Part, Matrix Bone)>>(
            ReferenceEqualityComparer.Instance);
        var groupOrder = new List<Texture2D>();
        List<(ModelMeshPart, Matrix)> nullGroup = null;

        int textureIndex = 0;
        foreach (ModelMesh mesh in model.Meshes)
        {
            Matrix bone = bones[mesh.ParentBone.Index];
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                Texture2D texture = groupByTexture && textureIndex < textures.Count
                    ? textures[textureIndex]
                    : null;
                textureIndex++;

                if (texture == null)
                {
                    nullGroup ??= new List<(ModelMeshPart, Matrix)>();
                    nullGroup.Add((part, bone));
                    continue;
                }
                if (!groups.TryGetValue(texture, out var list))
                {
                    list = new List<(ModelMeshPart, Matrix)>();
                    groups[texture] = list;
                    groupOrder.Add(texture);
                }
                list.Add((part, bone));
            }
        }

        var result = new MergedShadowMesh();
        if (nullGroup != null)
            result.BuildGroup(graphicsDevice, nullGroup, null, vertexCache, indexCache);
        foreach (Texture2D texture in groupOrder)
            result.BuildGroup(graphicsDevice, groups[texture], texture, vertexCache, indexCache);
        return result;
    }

    private void BuildGroup(
        GraphicsDevice graphicsDevice,
        List<(ModelMeshPart Part, Matrix Bone)> parts,
        Texture2D texture,
        Dictionary<VertexBuffer, (Vector3[] Positions, Vector2[] UVs)> vertexCache,
        Dictionary<IndexBuffer, int[]> indexCache)
    {
        var vertices = new List<VertexPositionTexture>();
        var indices = new List<ushort>();
        var remap = new Dictionary<int, ushort>();

        foreach (var (part, bone) in parts)
        {
            var (positions, uvs) = ReadVertices(part.VertexBuffer, vertexCache);
            int[] sourceIndices = ReadIndices(part.IndexBuffer, indexCache);
            remap.Clear();

            for (int t = 0; t < part.PrimitiveCount; t++)
            {
                // Si el triángulo podría no entrar, cerrar el lote actual.
                if (vertices.Count + 3 > MaxVerticesPerBatch)
                {
                    Flush(graphicsDevice, vertices, indices, texture);
                    remap.Clear();
                }

                for (int k = 0; k < 3; k++)
                {
                    int source = part.VertexOffset + sourceIndices[part.StartIndex + t * 3 + k];
                    if (!remap.TryGetValue(source, out ushort target))
                    {
                        target = (ushort)vertices.Count;
                        vertices.Add(new VertexPositionTexture(
                            Vector3.Transform(positions[source], bone),
                            uvs[source]));
                        remap[source] = target;
                    }
                    indices.Add(target);
                }
            }
        }

        Flush(graphicsDevice, vertices, indices, texture);
    }

    private void Flush(
        GraphicsDevice graphicsDevice,
        List<VertexPositionTexture> vertices,
        List<ushort> indices,
        Texture2D texture)
    {
        if (indices.Count == 0)
            return;

        var vb = new VertexBuffer(graphicsDevice, VertexPositionTexture.VertexDeclaration,
            vertices.Count, BufferUsage.WriteOnly);
        vb.SetData(vertices.ToArray());
        var ib = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits,
            indices.Count, BufferUsage.WriteOnly);
        ib.SetData(indices.ToArray());

        Batches.Add(new Batch
        {
            Vertices = vb,
            Indices = ib,
            PrimitiveCount = indices.Count / 3,
            Texture = texture,
        });
        vertices.Clear();
        indices.Clear();
    }

    private static (Vector3[] Positions, Vector2[] UVs) ReadVertices(
        VertexBuffer buffer,
        Dictionary<VertexBuffer, (Vector3[] Positions, Vector2[] UVs)> cache)
    {
        if (cache.TryGetValue(buffer, out var cached))
            return cached;

        VertexDeclaration declaration = buffer.VertexDeclaration;
        int stride = declaration.VertexStride;
        int positionOffset = -1;
        int uvOffset = -1;
        foreach (VertexElement element in declaration.GetVertexElements())
        {
            if (element.VertexElementUsage == VertexElementUsage.Position && element.UsageIndex == 0)
                positionOffset = element.Offset;
            else if (element.VertexElementUsage == VertexElementUsage.TextureCoordinate && element.UsageIndex == 0)
                uvOffset = element.Offset;
        }
        if (positionOffset < 0)
            throw new InvalidOperationException("El vertex buffer no tiene posición.");

        byte[] raw = new byte[buffer.VertexCount * stride];
        buffer.GetData(raw);

        var positions = new Vector3[buffer.VertexCount];
        var uvs = new Vector2[buffer.VertexCount];
        for (int i = 0; i < buffer.VertexCount; i++)
        {
            int baseOffset = i * stride;
            int p = baseOffset + positionOffset;
            positions[i] = new Vector3(
                BitConverter.ToSingle(raw, p),
                BitConverter.ToSingle(raw, p + 4),
                BitConverter.ToSingle(raw, p + 8));
            if (uvOffset >= 0)
            {
                int u = baseOffset + uvOffset;
                uvs[i] = new Vector2(
                    BitConverter.ToSingle(raw, u),
                    BitConverter.ToSingle(raw, u + 4));
            }
        }

        var result = (positions, uvs);
        cache[buffer] = result;
        return result;
    }

    private static int[] ReadIndices(IndexBuffer buffer, Dictionary<IndexBuffer, int[]> cache)
    {
        if (cache.TryGetValue(buffer, out var cached))
            return cached;

        int[] result = new int[buffer.IndexCount];
        if (buffer.IndexElementSize == IndexElementSize.SixteenBits)
        {
            var shorts = new ushort[buffer.IndexCount];
            buffer.GetData(shorts);
            for (int i = 0; i < shorts.Length; i++)
                result[i] = shorts[i];
        }
        else
        {
            buffer.GetData(result);
        }

        cache[buffer] = result;
        return result;
    }

    public void Dispose()
    {
        foreach (Batch batch in Batches)
        {
            batch.Vertices.Dispose();
            batch.Indices.Dispose();
        }
        Batches.Clear();
    }
}
