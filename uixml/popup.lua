class popup : popup_Designer with Modal
{
    popup(title, contents, buttons, callback, npcDialog)
    {
        base()
        this.ModalInit();
        if (npcDialog != nil) {
            this.InitNpcDialog(npcDialog);
            return;
        }
        var e = this.Elements;
        if((title ?? 0) == 0) {
			e.title.Visible = false;
		} else {
			e.title.Strid = title;
		}

        e.contents.SetString(StringFromID(contents ?? 0));
        if(buttons == 'ok') {
            e.ok_ok.Visible = true
            e.accept.Visible = false
            e.decline.Visible = false
			this.Widget.OnEscape(() => this.Close('ok'));
        } else {
			this.Widget.OnEscape(() => this.Close('decline'));
		}
        
        if (callback != nil) this.ModalCallback(callback);
        
        e.ok_ok.OnClick(() => this.Close('ok'));
        e.accept.OnClick(() => this.Close('accept'));
        e.decline.OnClick(() => this.Close('decline'));
    }

    InitNpcDialog(dialog)
    {
        local e = this.Elements;
        e.title.Text = StringFromID(dialog.IndividualName);
        local rumorOption = nil;
        local bribeOption = nil;
        local knowledgeOption = nil;
        local missionOption = nil;
        for (option in dialog.Options) {
            if (option.Kind == 3) {
                bribeOption = option;
                break;
            }
            if (option.Kind == 4 && knowledgeOption == nil)
                knowledgeOption = option;
            if (option.Kind == 1)
                rumorOption = option;
            if (option.Kind == 2)
                missionOption = option;
        }

        if (bribeOption != nil) {
            local bribeText = Game.FormatBaseNpcBribe(bribeOption.Contents, bribeOption.FactionIdsName, bribeOption.Price);
            e.contents.Visible = bribeText != nil && bribeText != "";
            if (e.contents.Visible)
                e.contents.SetString(bribeText);
            this.NpcAcceptDecline(bribeOption.Id);
            return;
        }

        if (knowledgeOption != nil) {
            local infoText = Game.FormatBaseNpcKnowledge(knowledgeOption.Text, knowledgeOption.ObjectNames, knowledgeOption.Price);
            e.contents.Visible = infoText != nil && infoText != "";
            if (e.contents.Visible)
                e.contents.SetString(infoText);
            this.NpcAcceptDecline(knowledgeOption.Id);
            return;
        }

        if (rumorOption != nil) {
            local rumorText = rumorOption.Contents != 0 ? rumorOption.Contents : dialog.Contents;
            e.contents.Visible = rumorText != 0;
            if (rumorText != 0)
                e.contents.SetInfocards({ GetInfocard(rumorText) });
            e.accept.X = 0;
            e.accept.Visible = true;
            e.decline.Visible = false;
            e.accept.OnClick(() => {
                if (this.Accepting) return;
                this.Accepting = true;
                e.accept.Enabled = false;
                this.Close();
                Game.BaseNpcOption(rumorOption.Id);
            });
            this.Widget.OnEscape(() => this.Close());
            return;
        }

        if (missionOption != nil) {
            e.contents.Visible = false;
            e.accept.X = 0;
            e.accept.Visible = true;
            e.accept.Strid = missionOption.Text != 0 ? missionOption.Text : 1350;
            e.decline.Visible = false;
            e.accept.OnClick(() => {
                this.Close();
                Game.OpenBaseAction("mission");
            });
            this.Widget.OnEscape(() => this.Close());
            return;
        }

        e.contents.Visible = dialog.Contents != 0;
        if (dialog.Contents != 0)
            e.contents.SetString(StringFromID(dialog.Contents));
        e.ok_ok.Visible = true;
        e.accept.Visible = false;
        e.decline.Visible = false;
        e.ok_ok.OnClick(() => this.Close());
        this.Widget.OnEscape(() => this.Close());
    }

    NpcAcceptDecline(optionId)
    {
        local e = this.Elements;
        e.accept.Visible = true;
        e.decline.Visible = true;
        e.accept.OnClick(() => {
            if (this.Accepting) return;
            this.Accepting = true;
            e.accept.Enabled = false;
            this.Close();
            Game.BaseNpcOption(optionId);
        });
        e.decline.OnClick(() => this.Close());
        this.Widget.OnEscape(() => this.Close());
    }
}
