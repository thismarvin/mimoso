using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Relatus.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Relatus.Graphics
{
    public enum SpriteMirroringType
    {
        None = 0,
        /// <summary>
        /// Reverse the x-axis of the sprite.
        /// </summary>
        FlipHorizontally = 1,
        /// <summary>
        /// Reverse the y-axis of the sprite.
        /// </summary>
        FlipVertically = 2
    }

    public class BetterSprite
    {
        public Texture2D Texture
        {
            get => texture;
            set => SetTexture(value);
        }
        public float X
        {
            get => x;
            set => SetPosition(value, y, z);
        }
        public float Y
        {
            get => y;
            set => SetPosition(x, value, z);
        }
        public float Z
        {
            get => z;
            set => SetPosition(x, y, value);
        }
        public Vector3 Translation
        {
            get => translation;
            set => SetTranslation(value.X, value.Y, value.Z);
        }
        public Vector3 Scale
        {
            get => scale;
            set => SetScale(value.X, value.Y, value.Z);
        }
        public Vector2 RotationOffset
        {
            get => rotationOffset;
            set => SetRotationOffset(value.X, value.Y);
        }
        public float Rotation
        {
            get => rotation;
            set => SetRotation(value);
        }
        public Color Tint
        {
            get => tint;
            set => SetTint(value);
        }
        public ImageRegion SampleRegion
        {
            get => sampleRegion;
            set => SetSampleRegion((int)value.X, (int)value.Y, (int)value.Width, (int)value.Height);
        }
        public SpriteMirroringType SpriteMirroring
        {
            get => spriteMirroring;
            set => SetSpriteMirroring(value);
        }
        public RenderOptions RenderOptions
        {
            get => renderOptions;
            set => SetRenderOptions(value);
        }

        public int Width => SampleRegion.Width;
        public int Height => SampleRegion.Height;

        public Vector3 Position
        {
            get => new Vector3(x, y, z);
            set => SetPosition(value.X, value.Y, value.Z);
        }

        // I just want to note that although this is a Vector3, it is really treated like a Vector2.
        public Vector3 Center
        {
            get => new Vector3(x + Width / 2, y - Height / 2, z);
            set => SetCenter(value.X, value.Y);
        }

        private Texture2D texture;
        private float texelWidth;
        private float texelHeight;

        private float x;
        private float y;
        private float z;
        private Vector3 translation;
        private Vector3 scale;
        private Vector2 rotationOffset;
        private float rotation;
        private Color tint;
        private ImageRegion sampleRegion;
        private SpriteMirroringType spriteMirroring;
        private RenderOptions renderOptions;

        private bool modelChanged;
        private bool textureChanged;
        private DynamicVertexBuffer modelBuffer;
        private DynamicVertexBuffer textureBuffer;
        private VertexBufferBinding[] vertexBufferBindings;

        private static readonly GraphicsDevice graphicsDevice;
        private static readonly GeometryData geometry;
        private static readonly Effect spriteShader;
        private static readonly EffectPass spritePass;

        static BetterSprite()
        {
            graphicsDevice = Engine.Graphics.GraphicsDevice;
            geometry = GeometryManager.GetShapeData(ShapeType.Square);
            spriteShader = AssetManager.GetEffect("Relatus_SpriteShader");
            spritePass = spriteShader.CurrentTechnique.Passes[0];
        }

        public BetterSprite()
        {
            tint = Color.White;
            scale = Vector3.One;
            renderOptions = new RenderOptions();

            modelChanged = true;
            textureChanged = true;
        }

        public static BetterSprite Create()
        {
            return new BetterSprite();
        }

        public static BetterSprite CreateFromAtlas(SpriteAtlas spriteAtlas, string name)
        {
            SpriteAtlasEntry entry = spriteAtlas.GetEntry(name);

            return Create()
                .SetTexture(spriteAtlas.GetPage(entry.Page))
                .SetSampleRegion(entry.ImageRegion)
                .ApplyChanges();
        }

        public virtual BetterSprite SetTexture(Texture2D texture)
        {
            this.texture = texture;

            texelWidth = 1f / texture.Width;
            texelHeight = 1f / texture.Height;

            textureChanged = true;

            return this;
        }

        public virtual BetterSprite SetPosition(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetTranslation(float x, float y, float z)
        {
            translation = new Vector3(x, y, z);

            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetScale(float x, float y, float z)
        {
            scale = new Vector3(x, y, z);

            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetRotationOffset(float x, float y)
        {
            rotationOffset = new Vector2(x, y);

            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetRotation(float rotation)
        {
            this.rotation = rotation;

            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetTint(Color tint)
        {
            this.tint = tint;

            textureChanged = true;

            return this;
        }

        public virtual BetterSprite SetSampleRegion(int x, int y, int width, int height)
        {
            sampleRegion = new ImageRegion(x, y, width, height);

            textureChanged = true;
            modelChanged = true;

            return this;
        }

        public virtual BetterSprite SetSampleRegion(ImageRegion region)
        {
            return SetSampleRegion(region.X, region.Y, region.Width, region.Height);
        }

        public virtual BetterSprite SetSpriteMirroring(SpriteMirroringType mirroringType)
        {
            spriteMirroring = mirroringType;

            textureChanged = true;

            return this;
        }

        public virtual BetterSprite SetRenderOptions(RenderOptions options)
        {
            renderOptions = options;

            return this;
        }

        public BetterSprite ApplyChanges()
        {
            if (textureChanged)
            {
                Vector2 topLeft = new Vector2((float)MathExt.RemapRange(sampleRegion.X, 0, texture.Width, 0, 1), (float)MathExt.RemapRange(sampleRegion.Y, 0, texture.Height, 0, 1));
                Vector2 topRight = topLeft + new Vector2(texelWidth * sampleRegion.Width, 0);
                Vector2 bottomRight = topLeft + new Vector2(texelWidth * sampleRegion.Width, texelHeight * sampleRegion.Height);
                Vector2 bottomLeft = topLeft + new Vector2(0, texelHeight * sampleRegion.Height);

                Vector2[] corners = new Vector2[] { topLeft, bottomLeft, bottomRight, topRight };

                textureBuffer?.Dispose();
                textureBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexColorTexture), 4, BufferUsage.WriteOnly);

                if ((spriteMirroring & SpriteMirroringType.FlipHorizontally) != SpriteMirroringType.None)
                {
                    Vector2 temp = corners[0];
                    corners[0] = corners[3];
                    corners[3] = temp;

                    temp = corners[1];
                    corners[1] = corners[2];
                    corners[2] = temp;
                }

                if ((spriteMirroring & SpriteMirroringType.FlipVertically) != SpriteMirroringType.None)
                {
                    Vector2 temp = corners[0];
                    corners[0] = corners[1];
                    corners[1] = temp;

                    temp = corners[3];
                    corners[3] = corners[2];
                    corners[2] = temp;
                }

                textureBuffer.SetData(new VertexColorTexture[]
                {
                    new VertexColorTexture(tint, corners[0]),
                    new VertexColorTexture(tint, corners[1]),
                    new VertexColorTexture(tint, corners[2]),
                    new VertexColorTexture(tint, corners[3]),
                });
            }

            if (modelChanged)
            {
                modelBuffer?.Dispose();
                modelBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexTransform), 1, BufferUsage.WriteOnly);
                modelBuffer.SetData(new VertexTransform[] { GetVertexTransformColor() });
            }

            vertexBufferBindings = new VertexBufferBinding[]
            {
                new VertexBufferBinding(modelBuffer, 0, 1),
                new VertexBufferBinding(textureBuffer),
                new VertexBufferBinding(geometry.VertexBuffer),
            };

            textureChanged = false;
            modelChanged = false;

            return this;
        }

        public BetterSprite SetCenter(float x, float y)
        {
            this.x = x - Width / 2;
            this.y = y + Height / 2;

            modelChanged = true;

            return this;
        }

        internal VertexTransform GetVertexTransformColor()
        {
            Vector3 scale = new Vector3(sampleRegion.Width * this.scale.X, sampleRegion.Height * this.scale.Y, this.scale.Z);
            Vector3 translation = new Vector3(x + this.translation.X, y + this.translation.Y, z + this.translation.Z);

            return new VertexTransform(scale, rotationOffset, rotation, translation);
        }

        public virtual void Draw(Camera camera)
        {
            if (textureChanged || modelChanged)
                throw new RelatusException("The sprite was modified, but ApplyChanges() was never called.", new MethodExpectedException());

            graphicsDevice.RasterizerState = GraphicsManager.RasterizerState;
            graphicsDevice.SamplerStates[0] = renderOptions.SamplerState;
            graphicsDevice.BlendState = renderOptions.BlendState;
            graphicsDevice.DepthStencilState = renderOptions.DepthStencilState;
            graphicsDevice.SetVertexBuffers(vertexBufferBindings);
            graphicsDevice.Indices = geometry.IndexBuffer;

            spriteShader.Parameters["SpriteTexture"].SetValue(texture);
            spriteShader.Parameters["WorldViewProjection"].SetValue(camera.WVP);

            spritePass.Apply();

            if (renderOptions.Effect == null)
            {
                graphicsDevice.Textures[0] = texture;
                graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, geometry.TotalTriangles, 1);
            }
            else
            {
                foreach (EffectPass pass in renderOptions.Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.Textures[0] = texture;
                    graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, geometry.TotalTriangles, 1);
                }
            }
        }
    }
}
