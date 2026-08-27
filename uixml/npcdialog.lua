class npcdialog : npcdialog_Designer with Modal
{
    npcdialog(dialog)
    {
        base();
        this.ModalInit();
        local e = this.Elements;
        e.title.Text = StringFromID(dialog.IndividualName);

        local rumorOption = nil;
        local bribeOption = nil;
        local knowledgeOption = nil;
        for (option in dialog.Options) {
            if (option.Kind == 3) {
                bribeOption = option;
                break;
            }
            if (option.Kind == 4 && knowledgeOption == nil) {
                knowledgeOption = option;
            }
            if (option.Kind == 1) {
                rumorOption = option;
            }
        }

        if (bribeOption != nil) {
            e.contents.Visible = bribeOption.Contents != 0;
            if (bribeOption.Contents != 0) {
                local bribeText = Game.FormatBaseNpcBribe(bribeOption.Contents, bribeOption.FactionIdsName, bribeOption.Price);
                e.contents.SetString(bribeText != nil && bribeText != "" ? bribeText : StringFromID(bribeOption.Contents));
            }
            e.close.Visible = false;
            e.accept.X = -68;
            e.decline.X = 68;
            e.accept.Y = 30;
            e.decline.Y = 30;
            e.accept.Visible = true;
            e.decline.Visible = true;
            e.accept.OnClick(() => {
                if (this.Accepting) return;
                this.Accepting = true;
                e.accept.Enabled = false;
                this.Close();
                Game.BaseNpcOption(bribeOption.Id);
            });
            e.decline.OnClick(() => this.Close());
            this.Widget.OnEscape(() => this.Close());
            return;
        }

        if (knowledgeOption != nil) {
            local infoText = knowledgeOption.Text != 0
                ? Game.FormatBaseNpcKnowledge(knowledgeOption.Text, knowledgeOption.ObjectNames, knowledgeOption.Price)
                : "";
            e.contents.Visible = infoText != "";
            if (infoText != "")
                e.contents.SetString(infoText);
            e.close.Visible = false;
            e.accept.X = -68;
            e.decline.X = 68;
            e.accept.Y = 30;
            e.decline.Y = 30;
            e.accept.Visible = true;
            e.decline.Visible = true;
            e.accept.OnClick(() => {
                if (this.Accepting) return;
                this.Accepting = true;
                e.accept.Enabled = false;
                this.Close();
                Game.BaseNpcOption(knowledgeOption.Id);
            });
            e.decline.OnClick(() => this.Close());
            this.Widget.OnEscape(() => this.Close());
            return;
        }

        if (rumorOption != nil) {
            local rumorText = rumorOption.Contents != 0 ? rumorOption.Contents : dialog.Contents;
            e.contents.Visible = rumorText != 0;
            if (rumorText != 0)
                e.contents.SetInfocards({ GetInfocard(rumorText) });
            e.close.Visible = false;
            e.accept.X = 0;
            e.accept.Y = 30;
            e.accept.Visible = true;
            e.decline.Visible = false;
            e.accept.OnClick(() => {
                Game.BaseNpcOption(rumorOption.Id);
                this.Close();
            });
            this.Widget.OnEscape(() => this.Close());
            return;
        }

        e.contents.Visible = dialog.Contents != 0;
        if (dialog.Contents != 0)
            e.contents.SetString(StringFromID(dialog.Contents));

        local ids = { "option1", "option2", "option3", "option4", "option5", "option6" };
        local index = 1;
        for (option in dialog.Options) {
            if (index > ids.length)
                break;
            local button = e[ids[index]];
            button.Visible = true;
            button.Text = option.Text != 0 ? StringFromID(option.Text) : StringFromID(1350);
            if (option.Price > 0)
                button.Text = button.Text + " - " + StringFromID(STRID_CREDIT_SIGN) + NumberToStringCS(option.Price, "N0");
            button.OnClick(() => {
                if (option.Kind == 2)
                    Game.OpenBaseAction("mission");
                else
                    Game.BaseNpcOption(option.Id);
                this.Close();
            });
            index += 1;
        }

        e.accept.X = 0;
        e.decline.X = 0;
        e.accept.Y = 30;
        e.decline.Y = 30;
        e.accept.Visible = false;
        e.decline.Visible = false;
        e.close.OnClick(() => this.Close());
        this.Widget.OnEscape(() => this.Close());
    }
}
