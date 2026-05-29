using System;

namespace LibreLancer.Interface;

public class ViewStateMachine<T> where T : struct, Enum
{
    public T State;

    enum ViewTransition
    {
        FadeIn,
        FadeOut,
        None
    }
    ViewTransition transition;
    double duration;
    double elapsed;

    private float nextFadeInDuration;
    private T nextState;

    public ViewStateMachine(T initial)
    {
        State = initial;
    }

    public bool Active(T state) => state.Equals(State) && transition == ViewTransition.None;
    public float Alpha(T state) => !state.Equals(State) ? 0 : transition switch
    {
        ViewTransition.FadeIn => MathHelper.Clamp((float)(elapsed / duration), 0, 1),
        ViewTransition.FadeOut => 1 - MathHelper.Clamp((float)(elapsed / duration), 0, 1),
        _ => 1f,
    };

    public void Reset(T state)
    {
        State = state;
        transition = ViewTransition.None;
    }

    public void Switch(T target, float fadeOut, float fadeIn)
    {
        transition = ViewTransition.FadeOut;
        duration = fadeOut;
        nextFadeInDuration = fadeIn;
        nextState = target;
        elapsed = 0;
    }

    public T? Update(double delta)
    {
        if (transition == ViewTransition.None)
            return null;
        if (transition == ViewTransition.FadeIn)
        {
            elapsed += delta;
            if (elapsed > duration)
            {
                transition = ViewTransition.None;
            }
        }
        if (transition == ViewTransition.FadeOut)
        {
            elapsed += delta;
            if (elapsed > duration)
            {
                State = nextState;
                duration = nextFadeInDuration;
                elapsed = 0;
                transition = ViewTransition.FadeIn;
                return State;
            }
        }
        return null;
    }
}
