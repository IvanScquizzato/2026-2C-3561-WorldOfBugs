using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Renderers;

/// <summary>
/// Versión unificada de un Model para el pase principal. A diferencia de
/// MergedShadowMesh, conserva el formato de vértice original (normales,
/// tangentes, UV, etc.) copiando los bytes tal cual y aplicando la
/// transformación del hueso a posición, normal, tangente y binormal.
///
/// Las partes se agrupan por textura (cada textura implica su propio normal
/// map, técnica y material) y por formato de vértice. Cada grupo se parte en
/// lotes de hasta 65535 vértices porque el perfil Reach solo admite índices
/// de 16 bits.
/// </summary>
public sealed class MergedModelMesh : IDisposable
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

    private sealed class VertexSource
    {
        public byte[] Raw;
        public VertexDeclaration Declaration;
        public int Stride;
    }

    private sealed class Group
    {
        public Texture2D Texture;
        public VertexSource Format;
        public List<(ModelMeshPart Part, Matrix Bone)> Parts = new();
    }

    /// <summary>
    /// Devuelve null si el modelo usa un formato de vértice que no se sabe
    /// transformar; en ese caso hay que dibujarlo por el camino de siempre.
    /// </summary>
    public static MergedModelMesh TryBuild(GraphicsDevice graphicsDevice, Model model, List<Texture2D> textures)
    {
        var bones = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(bones);

        var vertexCache = new Dictionary<VertexBuffer, VertexSource>();
        var indexCache = new Dictionary<IndexBuffer, int[]>();
        var groups = new Dictionary<(Texture2D, string), Group>();
        var groupOrder = new List<Group>();

        int textureIndex = 0;
        foreach (ModelMesh mesh in model.Meshes)
        {
            Matrix bone = bones[mesh.ParentBone.Index];
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                Texture2D texture = textureIndex < textures.Count ? textures[textureIndex] : null;
                textureIndex++;

                VertexSource source = ReadVertices(part.VertexBuffer, vertexCache);
                if (bone != Matrix.Identity && !CanTransform(source.Declaration))
                    return null;

                var key = (texture, DeclarationKey(source.Declaration));
                if (!groups.TryGetValue(key, out Group group))
                {
                    group = new Group { Texture = texture, Format = source };
                    groups[key] = group;
                    groupOrder.Add(group);
                }
                group.Parts.Add((part, bone));
            }
        }

        var result = new MergedModelMesh();
        foreach (Group group in groupOrder)
            result.BuildGroup(graphicsDevice, group, vertexCache, indexCache);
        return result;
    }

    private void BuildGroup(
        GraphicsDevice graphicsDevice,
        Group group,
        Dictionary<VertexBuffer, VertexSource> vertexCache,
        Dictionary<IndexBuffer, int[]> indexCache)
    {
        int stride = group.Format.Stride;
        VertexElement[] elements = group.Format.Declaration.GetVertexElements();
        byte[] vertices = new byte[MaxVerticesPerBatch * stride];
        int vertexCount = 0;
        var indices = new List<ushort>();
        var remap = new Dictionary<int, ushort>();

        foreach (var (part, bone) in group.Parts)
        {
            VertexSource source = ReadVertices(part.VertexBuffer, vertexCache);
            int[] sourceIndices = ReadIndices(part.IndexBuffer, indexCache);
            bool transform = bone != Matrix.Identity;
            remap.Clear();

            for (int t = 0; t < part.PrimitiveCount; t++)
            {
                if (vertexCount + 3 > MaxVerticesPerBatch)
                {
                    Flush(graphicsDevice, group, vertices, vertexCount, indices);
                    vertexCount = 0;
                    remap.Clear();
                }

                for (int k = 0; k < 3; k++)
                {
                    int src = part.VertexOffset + sourceIndices[part.StartIndex + t * 3 + k];
                    if (!remap.TryGetValue(src, out ushort target))
                    {
                        target = (ushort)vertexCount;
                        int dstOffset = vertexCount * stride;
                        Buffer.BlockCopy(source.Raw, src * stride, vertices, dstOffset, stride);
                        if (transform)
                            TransformVertex(vertices, dstOffset, elements, bone);
                        vertexCount++;
                        remap[src] = target;
                    }
                    indices.Add(target);
                }
            }
        }

        Flush(graphicsDevice, group, vertices, vertexCount, indices);
    }

    private void Flush(GraphicsDevice graphicsDevice, Group group, byte[] vertices, int vertexCount, List<ushort> indices)
    {
        if (indices.Count == 0)
            return;

        int stride = group.Format.Stride;
        var declaration = new VertexDeclaration(stride, group.Format.Declaration.GetVertexElements());
        var vb = new VertexBuffer(graphicsDevice, declaration, vertexCount, BufferUsage.WriteOnly);
        vb.SetData(vertices, 0, vertexCount * stride);
        var ib = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        ib.SetData(indices.ToArray());

        Batches.Add(new Batch
        {
            Vertices = vb,
            Indices = ib,
            PrimitiveCount = indices.Count / 3,
            Texture = group.Texture,
        });
        indices.Clear();
    }

    // ---- Transformación de vértices -------------------------------------

    private static bool IsTransformedUsage(VertexElementUsage usage) =>
        usage == VertexElementUsage.Position ||
        usage == VertexElementUsage.Normal ||
        usage == VertexElementUsage.Tangent ||
        usage == VertexElementUsage.Binormal;

    private static bool CanTransform(VertexDeclaration declaration)
    {
        foreach (VertexElement element in declaration.GetVertexElements())
        {
            if (IsTransformedUsage(element.VertexElementUsage) &&
                element.VertexElementFormat != VertexElementFormat.Vector3 &&
                element.VertexElementFormat != VertexElementFormat.Vector4)
                return false;
        }
        return true;
    }

    private static void TransformVertex(byte[] data, int vertexOffset, VertexElement[] elements, Matrix bone)
    {
        foreach (VertexElement element in elements)
        {
            if (!IsTransformedUsage(element.VertexElementUsage))
                continue;

            int offset = vertexOffset + element.Offset;
            Vector3 value = ReadVector3(data, offset);
            if (element.VertexElementUsage == VertexElementUsage.Position)
            {
                value = Vector3.Transform(value, bone);
            }
            else
            {
                value = Vector3.TransformNormal(value, bone);
                if (value.LengthSquared() > 0f)
                    value.Normalize();
            }
            // Para Vector4 solo se toca xyz: w (p. ej. el signo de la tangente) queda igual.
            WriteVector3(data, offset, value);
        }
    }

    private static Vector3 ReadVector3(byte[] data, int offset) => new(
        BitConverter.ToSingle(data, offset),
        BitConverter.ToSingle(data, offset + 4),
        BitConverter.ToSingle(data, offset + 8));

    private static void WriteVector3(byte[] data, int offset, Vector3 value)
    {
        BitConverter.TryWriteBytes(new Span<byte>(data, offset, 4), value.X);
        BitConverter.TryWriteBytes(new Span<byte>(data, offset + 4, 4), value.Y);
        BitConverter.TryWriteBytes(new Span<byte>(data, offset + 8, 4), value.Z);
    }

    // ---- Lectura de buffers ----------------------------------------------

    private static string DeclarationKey(VertexDeclaration declaration)
    {
        var sb = new StringBuilder();
        sb.Append(declaration.VertexStride);
        foreach (VertexElement e in declaration.GetVertexElements())
            sb.Append('|').Append(e.Offset).Append(',').Append(e.VertexElementFormat)
              .Append(',').Append(e.VertexElementUsage).Append(',').Append(e.UsageIndex);
        return sb.ToString();
    }

    private static VertexSource ReadVertices(VertexBuffer buffer, Dictionary<VertexBuffer, VertexSource> cache)
    {
        if (cache.TryGetValue(buffer, out VertexSource cached))
            return cached;

        int stride = buffer.VertexDeclaration.VertexStride;
        byte[] raw = new byte[buffer.VertexCount * stride];
        buffer.GetData(raw);

        var source = new VertexSource { Raw = raw, Declaration = buffer.VertexDeclaration, Stride = stride };
        cache[buffer] = source;
        return source;
    }

    private static int[] ReadIndices(IndexBuffer buffer, Dictionary<IndexBuffer, int[]> cache)
    {
        if (cache.TryGetValue(buffer, out int[] cached))
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
