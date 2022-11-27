// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client;

namespace LibreLancer
{
    //Blank state for when CGameSession is waiting for the server to spawn the player
    public class NetWaitState : GameState
    {
        private CGameSession session;
        public NetWaitState(CGameSession session, FreelancerGame game) : base(game)
        {
            this.session = session;
        }

        public override void Update(double delta)
        {
            session.WaitStart();
        }

        public override void Draw(double delta)
        {
            Game.RenderContext.ClearColor = Color4.Black;
            Game.RenderContext.ClearAll();
        }

        public override void Exiting()
        {
            session.OnExit();
        }
    }
}