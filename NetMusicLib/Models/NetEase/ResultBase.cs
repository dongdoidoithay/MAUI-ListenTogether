﻿namespace NetMusicLib.Models.NetEase;
internal class ResultBase<T>
{
    public List<T> data { get; set; } = null!;
    public T result { get; set; } = default(T)!;
    public int code { get; set; }
    public string msg { get; set; } = null!;
}