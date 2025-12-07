#if CLIENT
using System.Drawing;
using SCEFGUI.Core;
using SCEFGUI.Tween;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public delegate void PlayCompleteCallback();

public class FGUITransition
{
    public string Name { get; set; } = "";
    public FGUIComponent? Owner { get; set; }

    private List<TransitionItem> _items = new();
    private int _totalTimes;
    private int _totalTasks;
    private bool _playing;
    private bool _paused;
    private float _ownerBaseX;
    private float _ownerBaseY;
    private PlayCompleteCallback? _onComplete;
    private bool _reversed;
    private float _totalDuration;
    private bool _autoPlay;
    private int _autoPlayTimes = 1;
    private float _autoPlayDelay;
    private float _timeScale = 1;
    private float _startTime;
    private float _endTime = -1;

    public bool Playing => _playing;
    public bool Paused { get => _paused; set => _paused = value; }
    public float TimeScale { get => _timeScale; set => _timeScale = value; }

    public void Play(PlayCompleteCallback? onComplete = null, int times = 1, float delay = 0)
    {
        PlayInternal(times, delay, 0, -1, onComplete, false);
    }

    public void PlayReverse(PlayCompleteCallback? onComplete = null, int times = 1, float delay = 0)
    {
        PlayInternal(times, delay, 0, -1, onComplete, true);
    }

    private void PlayInternal(int times, float delay, float startTime, float endTime, PlayCompleteCallback? onComplete, bool reversed)
    {
        Stop(true, false);
        _totalTimes = times;
        _reversed = reversed;
        _startTime = startTime;
        _endTime = endTime;
        _playing = true;
        _paused = false;
        _onComplete = onComplete;

        if (Owner != null)
        {
            _ownerBaseX = Owner.X;
            _ownerBaseY = Owner.Y;
        }

        _totalTasks = 0;
        bool needSkipAnimations = false;
        int cnt = _items.Count;

        if (delay == 0)
        {
            for (int i = 0; i < cnt; i++)
            {
                var item = _items[i];
                if (item.Target == null) continue;

                if (item.Type == TransitionActionType.Animation && startTime != 0)
                    needSkipAnimations = true;

                PlayItem(item);
            }
        }
        else
        {
            GTween.DelayedCall(delay, () =>
            {
                for (int i = 0; i < cnt; i++)
                {
                    var item = _items[i];
                    if (item.Target == null) continue;
                    PlayItem(item);
                }
            });
        }
    }

    private void PlayItem(TransitionItem item)
    {
        if (item.TweenConfig != null)
        {
            float startTime = _reversed ? (_totalDuration - item.Time - item.TweenConfig.Duration) : item.Time;
            if (_endTime >= 0 && startTime > _endTime) return;

            _totalTasks++;
            float delay = startTime > _startTime ? startTime - _startTime : 0;
            StartTween(item, delay);
        }
        else
        {
            float time = _reversed ? (_totalDuration - item.Time) : item.Time;
            if (time <= _startTime)
                ApplyValue(item);
            else if (_endTime < 0 || time <= _endTime)
            {
                _totalTasks++;
                float delay = time - _startTime;
                GTween.DelayedCall(delay, () =>
                {
                    _totalTasks--;
                    ApplyValue(item);
                    CheckAllComplete();
                });
            }
        }
    }

    private void StartTween(TransitionItem item, float delay)
    {
        if (item.TweenConfig == null || item.Target == null) return;

        var tweener = GTween.To(0f, 1f, item.TweenConfig.Duration)
            .SetDelay(delay)
            .SetEase(item.TweenConfig.EaseType)
            .SetTarget(item)
            .OnUpdate(t => OnTweenUpdate(item, t.NormalizedTime))
            .OnComplete(t => OnTweenComplete(item));
    }

    private void OnTweenUpdate(TransitionItem item, float ratio)
    {
        if (item.Target == null) return;
        ApplyValue(item, ratio);
    }

    private void OnTweenComplete(TransitionItem item)
    {
        _totalTasks--;
        CheckAllComplete();
    }

    private void ApplyValue(TransitionItem item, float ratio = 1)
    {
        if (item.Target == null) return;
        var target = item.Target;

        switch (item.Type)
        {
            case TransitionActionType.XY:
                {
                    var start = item.StartValue;
                    var end = item.EndValue;
                    float x = start.X + (end.X - start.X) * ratio;
                    float y = start.Y + (end.Y - start.Y) * ratio;
                    if (item.TweenConfig?.Path != null)
                    {
                        // Path animation - simplified
                    }
                    target.SetXY(x, y);
                }
                break;
            case TransitionActionType.Size:
                {
                    var start = item.StartValue;
                    var end = item.EndValue;
                    float w = start.X + (end.X - start.X) * ratio;
                    float h = start.Y + (end.Y - start.Y) * ratio;
                    target.SetSize(w, h);
                }
                break;
            case TransitionActionType.Scale:
                {
                    var start = item.StartValue;
                    var end = item.EndValue;
                    float sx = start.X + (end.X - start.X) * ratio;
                    float sy = start.Y + (end.Y - start.Y) * ratio;
                    target.SetScale(sx, sy);
                }
                break;
            case TransitionActionType.Alpha:
                {
                    float a = item.StartValue.X + (item.EndValue.X - item.StartValue.X) * ratio;
                    target.Alpha = a;
                }
                break;
            case TransitionActionType.Rotation:
                {
                    float r = item.StartValue.X + (item.EndValue.X - item.StartValue.X) * ratio;
                    target.Rotation = r;
                }
                break;
            case TransitionActionType.Visible:
                target.Visible = item.EndValue.B1;
                break;
            case TransitionActionType.Color:
                if (target is IColorGear cg)
                {
                    var startC = item.StartValue.C;
                    var endC = item.EndValue.C;
                    int r = (int)(startC.R + (endC.R - startC.R) * ratio);
                    int g = (int)(startC.G + (endC.G - startC.G) * ratio);
                    int b = (int)(startC.B + (endC.B - startC.B) * ratio);
                    int a = (int)(startC.A + (endC.A - startC.A) * ratio);
                    cg.Color = Color.FromArgb(a, r, g, b);
                }
                break;
            case TransitionActionType.Animation:
                if (target is FGUIMovieClip mc)
                {
                    mc.Frame = (int)item.StartValue.X;
                    mc.Playing = item.StartValue.B1;
                }
                break;
            case TransitionActionType.Pivot:
                {
                    float px = item.StartValue.X + (item.EndValue.X - item.StartValue.X) * ratio;
                    float py = item.StartValue.Y + (item.EndValue.Y - item.StartValue.Y) * ratio;
                    target.SetPivot(px, py, target.PivotAsAnchor);
                }
                break;
            case TransitionActionType.Text:
                target.Text = item.EndValue.S;
                break;
            case TransitionActionType.Icon:
                target.Icon = item.EndValue.S;
                break;
            case TransitionActionType.Shake:
                // Shake implementation
                break;
        }
    }

    private void CheckAllComplete()
    {
        if (_playing && _totalTasks == 0)
        {
            if (_totalTimes < 0)
            {
                // Infinite loop - restart
                PlayInternal(_totalTimes, 0, _startTime, _endTime, _onComplete, _reversed);
            }
            else
            {
                _totalTimes--;
                if (_totalTimes > 0)
                    PlayInternal(_totalTimes, 0, _startTime, _endTime, _onComplete, _reversed);
                else
                {
                    _playing = false;
                    _onComplete?.Invoke();
                }
            }
        }
    }

    public void Stop(bool setToComplete = true, bool processCallback = false)
    {
        if (!_playing) return;

        _playing = false;
        _totalTasks = 0;
        _totalTimes = 0;

        GTween.Kill(this);
        foreach (var item in _items)
            GTween.Kill(item);

        if (processCallback)
            _onComplete?.Invoke();
    }

    public void SetValue(string label, params object[] aParams)
    {
        foreach (var item in _items)
        {
            if (item.Label == label || item.Label2 == label)
            {
                if (aParams.Length == 0) continue;
                if (item.TweenConfig != null && item.Label == label)
                    SetItemStartValue(item, aParams);
                else
                    SetItemEndValue(item, aParams);
            }
        }
    }

    private void SetItemStartValue(TransitionItem item, object[] aParams)
    {
        switch (item.Type)
        {
            case TransitionActionType.XY:
            case TransitionActionType.Size:
            case TransitionActionType.Scale:
            case TransitionActionType.Pivot:
            case TransitionActionType.Skew:
                if (aParams.Length >= 2)
                {
                    item.StartValue.X = Convert.ToSingle(aParams[0]);
                    item.StartValue.Y = Convert.ToSingle(aParams[1]);
                }
                break;
            case TransitionActionType.Alpha:
            case TransitionActionType.Rotation:
                if (aParams.Length >= 1)
                    item.StartValue.X = Convert.ToSingle(aParams[0]);
                break;
            case TransitionActionType.Color:
                if (aParams.Length >= 1 && aParams[0] is Color c)
                    item.StartValue.C = c;
                break;
        }
    }

    private void SetItemEndValue(TransitionItem item, object[] aParams)
    {
        switch (item.Type)
        {
            case TransitionActionType.XY:
            case TransitionActionType.Size:
            case TransitionActionType.Scale:
            case TransitionActionType.Pivot:
            case TransitionActionType.Skew:
                if (aParams.Length >= 2)
                {
                    item.EndValue.X = Convert.ToSingle(aParams[0]);
                    item.EndValue.Y = Convert.ToSingle(aParams[1]);
                }
                break;
            case TransitionActionType.Alpha:
            case TransitionActionType.Rotation:
                if (aParams.Length >= 1)
                    item.EndValue.X = Convert.ToSingle(aParams[0]);
                break;
            case TransitionActionType.Color:
                if (aParams.Length >= 1 && aParams[0] is Color c)
                    item.EndValue.C = c;
                break;
            case TransitionActionType.Visible:
                if (aParams.Length >= 1)
                    item.EndValue.B1 = Convert.ToBoolean(aParams[0]);
                break;
            case TransitionActionType.Text:
            case TransitionActionType.Icon:
                if (aParams.Length >= 1)
                    item.EndValue.S = aParams[0]?.ToString();
                break;
        }
    }

    public void Setup(ByteBuffer buffer)
    {
        Name = buffer.ReadS() ?? "";
        int options = buffer.ReadInt();
        _autoPlay = (options & 1) != 0;
        _autoPlayTimes = buffer.ReadInt();
        _autoPlayDelay = buffer.ReadFloat();

        int cnt = buffer.ReadShort();
        for (int i = 0; i < cnt; i++)
        {
            int dataLen = buffer.ReadShort();
            int startPos = buffer.Position;

            var item = new TransitionItem();
            item.Time = buffer.ReadFloat();
            int targetId = buffer.ReadShort();
            if (targetId < 0)
                item.Target = Owner;
            else
                item.Target = Owner?.GetChildAt(targetId);

            item.Type = (TransitionActionType)buffer.ReadByte();
            item.TweenConfig = null;

            if (buffer.ReadBool())
            {
                item.TweenConfig = new TweenConfig
                {
                    Duration = buffer.ReadFloat(),
                    EaseType = (EaseType)buffer.ReadByte()
                };
                int repeat = buffer.ReadInt();
                if (repeat == 1) item.TweenConfig.Repeat = true;
                else if (repeat == 2) item.TweenConfig.Yoyo = true;
                item.TweenConfig.Repeat = repeat != 0;
                item.Label = buffer.ReadS();
            }

            item.Label2 = buffer.ReadS();

            switch (item.Type)
            {
                case TransitionActionType.XY:
                case TransitionActionType.Size:
                case TransitionActionType.Scale:
                case TransitionActionType.Pivot:
                case TransitionActionType.Skew:
                    item.StartValue.B1 = buffer.ReadBool();
                    item.StartValue.B2 = buffer.ReadBool();
                    item.StartValue.X = buffer.ReadFloat();
                    item.StartValue.Y = buffer.ReadFloat();
                    if (item.TweenConfig != null)
                    {
                        item.EndValue.B1 = buffer.ReadBool();
                        item.EndValue.B2 = buffer.ReadBool();
                        item.EndValue.X = buffer.ReadFloat();
                        item.EndValue.Y = buffer.ReadFloat();
                    }
                    if (buffer.Version >= 2 && item.Type == TransitionActionType.XY && buffer.ReadBool())
                    {
                        // Path data - skip for now
                        int pathLen = buffer.ReadShort();
                        buffer.Skip(pathLen * 14); // Approximate path point size
                    }
                    break;

                case TransitionActionType.Alpha:
                case TransitionActionType.Rotation:
                    item.StartValue.X = buffer.ReadFloat();
                    if (item.TweenConfig != null)
                        item.EndValue.X = buffer.ReadFloat();
                    break;

                case TransitionActionType.Color:
                    item.StartValue.C = buffer.ReadColor();
                    if (item.TweenConfig != null)
                        item.EndValue.C = buffer.ReadColor();
                    break;

                case TransitionActionType.Animation:
                    item.StartValue.X = buffer.ReadInt(); // frame
                    item.StartValue.B1 = buffer.ReadBool(); // playing
                    break;

                case TransitionActionType.Visible:
                    item.StartValue.B1 = buffer.ReadBool();
                    break;

                case TransitionActionType.Sound:
                    buffer.ReadS(); // sound
                    buffer.ReadFloat(); // volume
                    break;

                case TransitionActionType.Transition:
                    buffer.ReadS(); // transName
                    buffer.ReadInt(); // playTimes
                    break;

                case TransitionActionType.Shake:
                    item.StartValue.X = buffer.ReadFloat(); // amplitude
                    item.StartValue.Y = buffer.ReadFloat(); // duration
                    break;

                case TransitionActionType.ColorFilter:
                    for (int j = 0; j < 4; j++)
                        buffer.ReadFloat();
                    if (item.TweenConfig != null)
                        for (int j = 0; j < 4; j++)
                            buffer.ReadFloat();
                    break;

                case TransitionActionType.Text:
                case TransitionActionType.Icon:
                    item.StartValue.S = buffer.ReadS();
                    if (item.TweenConfig != null)
                        item.EndValue.S = buffer.ReadS();
                    break;
            }

            _items.Add(item);
            buffer.Position = startPos + dataLen;
        }

        // Calculate total duration
        _totalDuration = 0;
        foreach (var item in _items)
        {
            float endTime = item.Time;
            if (item.TweenConfig != null)
                endTime += item.TweenConfig.Duration;
            if (endTime > _totalDuration)
                _totalDuration = endTime;
        }
    }

    public void Dispose()
    {
        Stop(false, false);
        _items.Clear();
    }
}

class TransitionValue
{
    public float X, Y, Z, W;
    public bool B1, B2, B3, B4;
    public Color C = Color.White;
    public string? S;
    public int I;
}

class TweenConfig
{
    public float Duration;
    public EaseType EaseType = EaseType.QuadOut;
    public bool Repeat;
    public bool Yoyo;
    public object? Path; // For path animation
}

class TransitionItem
{
    public float Time;
    public FGUIObject? Target;
    public TransitionActionType Type;
    public TweenConfig? TweenConfig;
    public string? Label;
    public string? Label2;
    public TransitionValue StartValue = new();
    public TransitionValue EndValue = new();
}
#endif
