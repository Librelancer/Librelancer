// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Graphics.Backends.OpenGL;

internal static class GLBind
{
    private static uint programBound = 0;
    public static void UseProgram(uint prg)
    {
        if (programBound == prg)
        {
            return;
        }

        GL.UseProgram (prg);
        programBound = prg;
    }

    public static void Trash()
    {
        programBound = 0;
        bound_vao = 0;
        for(int i = 0; i < textures.Length; i++) { textures[i] = -1; };
        active_unit = -1;
    }

    private static int[] textures = [-1, -1, -1, -1, -1, -1, -1, -1];
    private static int active_unit = -1;
    public static void BindTexture(int unit, int target, uint texture)
    {
        var uval = GL.GL_TEXTURE0 + unit;

        if (textures[unit] == (int) texture)
        {
            return;
        }

        if (uval != active_unit)
        {
            GL.ActiveTexture(uval);
            active_unit = uval;
        }
        textures[unit] = (int)texture;
        GL.BindTexture(target, texture);
    }

    public static void BindTextureForModify(int target, uint texture)
    {
        if (active_unit != -1 && textures[active_unit - GL.GL_TEXTURE0] == texture)
            return;

        var uval = GL.GL_TEXTURE0 + 4;
        if (uval != active_unit)
        {
            GL.ActiveTexture(uval);
            active_unit = uval;
        }

        if (textures[4] == (int) texture)
        {
            return;
        }

        textures[4] = (int)texture;
        GL.BindTexture(target, texture);
    }

    private static uint bound_vao = 0;

    public static void VertexArray(uint vao)
    {
        if (bound_vao == vao)
        {
            return;
        }

        bound_vao = vao;
        GL.BindVertexArray (vao);
    }

    private static uint bound_ubo = 0;
    public static void UniformBuffer(uint ubo)
    {
        /*if (bound_ubo != ubo)
        {
            bound_ubo = ubo;
            GL.BindBuffer(GL.GL_UNIFORM_BUFFER, ubo);
        }*/
        GL.BindBuffer(GL.GL_UNIFORM_BUFFER, ubo);
    }
}
