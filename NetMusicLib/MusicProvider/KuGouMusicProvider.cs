﻿using System.Net;
using JiuLing.CommonLibs.ExtensionMethods;
using JiuLing.CommonLibs.Net;
using JiuLing.CommonLibs.Security;
using Microsoft.Extensions.Logging;
using NetMusicLib.Enums;
using NetMusicLib.Models;
using NetMusicLib.Models.KuGou;
using NetMusicLib.Utils;

namespace NetMusicLib.MusicProvider;
public class KuGouMusicProvider : IMusicProvider
{
    private readonly HttpClient _httpClient;
    public PlatformEnum Platform => PlatformEnum.KuGou;
    private readonly ILogger<KuGouMusicProvider> _logger;

    public KuGouMusicProvider(ILogger<KuGouMusicProvider> logger)
    {
        _logger = logger;
        var handler = new HttpClientHandler();
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<Music>> SearchAsync(string keyword)
    {
        var musics = new List<Music>();
        try
        {
            string args = KuGouUtils.GetSearchData(keyword);
            string url = $"{UrlBase.KuGou.Search}?{args}";

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            request.Headers.Add("User-Agent", BrowserDefaultHeader.EdgeUserAgent);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            json = KuGouUtils.RemoveHttpResultHead(json);
            if (json.IsEmpty())
            {
                return musics;
            }

            HttpResultBase<HttpMusicSearchResult>? httpResult;
            try
            {
                httpResult = json.ToObject<HttpResultBase<HttpMusicSearchResult>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析酷狗搜索结果失败。");
                return musics;
            }
            if (httpResult == null || httpResult.status != 1 || httpResult.error_code != 0)
            {
                return musics;
            }

            foreach (var httpMusic in httpResult.data.lists)
            {
                try
                {
                    var ExtendDataJson = (new KuGouSearchExtendData()
                    {
                        Hash = httpMusic.FileHash,
                        AlbumId = httpMusic.AlbumID
                    }).ToJson();

                    var music = new Music()
                    {
                        Id = MD5Utils.GetStringValueToLower($"{Platform}-{httpMusic.ID}"),
                        Platform = Platform,
                        IdOnPlatform = httpMusic.ID,
                        Name = KuGouUtils.RemoveSongNameTag(httpMusic.SongName),
                        Artist = KuGouUtils.RemoveSongNameTag(httpMusic.SingerName),
                        Album = httpMusic.AlbumName,
                        ImageUrl = "",
                        Duration = TimeSpan.FromSeconds(httpMusic.Duration),
                        Fee = GetFeeFlag(httpMusic.Privilege),
                        ExtendDataJson = ExtendDataJson
                    };
                    musics.Add(music);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "构建酷狗搜索结果失败。");

                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "酷狗搜索失败。");
        }
        return musics;
    }

    private FeeEnum GetFeeFlag(int Privilege)
    {
        if (Privilege == 10)
        {
            return FeeEnum.Demo;
        }
        return FeeEnum.Free;
    }

    public Task<List<string>> GetSearchSuggestAsync(string keyword)
    {
        throw new NotImplementedException();
    }

    public Task<List<SongMenu>> GetSongMenusFromTop()
    {
        throw new NotImplementedException();
    }

    public Task<List<Music>> GetTopMusicsAsync(string topId)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetHotWordAsync()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetShareUrlAsync(string id, string extendDataJson = "")
    {
        if (extendDataJson.IsEmpty())
        {
            return Task.FromResult(UrlBase.KuGou.Index);
        }
        var extendData = extendDataJson.ToObject<KuGouSearchExtendData>();
        if (extendData == null)
        {
            return Task.FromResult(UrlBase.KuGou.Index);
        }
        return Task.FromResult($"{UrlBase.KuGou.GetMusicPlayPage}/#hash={extendData.Hash}&album_id={extendData.AlbumId}&album_audio_id={id}");
    }

    public async Task<string> GetLyricAsync(string id, string extendDataJson = "")
    {
        try
        {
            if (extendDataJson.IsEmpty())
            {
                return "";
            }
            var extendData = extendDataJson.ToObject<KuGouSearchExtendData>();
            if (extendData == null)
            {
                return "";
            }
            string args = KuGouUtils.GetMusicUrlData(extendData.Hash, extendData.AlbumId);
            string url = $"{UrlBase.KuGou.GetMusic}?{args}";

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            request.Headers.Add("User-Agent", BrowserDefaultHeader.EdgeUserAgent);
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (json.IsEmpty())
            {
                return "";
            }
            var httpResult = json.ToObject<HttpResultBase<MusicDetailHttpResult>>();
            if (httpResult == null)
            {
                return "";
            }
            if (httpResult.status != 1 || httpResult.error_code != 0)
            {
                return "";
            }

            return httpResult.data.lyrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "酷狗歌词获取失败。");
            return "";
        }
    }

    public Task<PlatformMusicTag?> GetMusicTagsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<SongMenu>> GetSongMenusFromTagAsync(string id, int page)
    {
        throw new NotImplementedException();
    }

    public Task<List<Music>> GetTagMusicsAsync(string tagId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetPlayUrlAsync(string id, string extendDataJson = "")
    {
        if (extendDataJson.IsEmpty())
        {
            _logger.LogInformation("更新酷狗播放地址失败，扩展数据不存在");
            return "";
        }

        var extendData = extendDataJson.ToObject<KuGouSearchExtendData>();
        if (extendData == null)
        {
            _logger.LogInformation("更新酷狗播放地址失败，扩展数据格式错误");
            return "";
        }
        try
        {
            string args = KuGouUtils.GetMusicUrlData(extendData.Hash, extendData.AlbumId);
            string url = $"{UrlBase.KuGou.GetMusic}?{args}";

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            request.Headers.Add("User-Agent", BrowserDefaultHeader.EdgeUserAgent);
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (json.IsEmpty())
            {
                _logger.LogInformation("更新酷狗播放地址失败，服务器返回空。");
                return "";
            }
            var httpResult = json.ToObject<HttpResultBase<MusicDetailHttpResult>>();
            if (httpResult == null)
            {
                _logger.LogInformation("更新酷狗播放地址失败，服务器数据格式不正确。");
                return "";
            }
            if (httpResult.status != 1 || httpResult.error_code != 0)
            {
                _logger.LogInformation("更新酷狗播放地址失败，服务器返回错误。");
                return "";
            }
            return httpResult.data.play_url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新酷狗播放地址失败");
            return "";
        }
    }

    public async Task<string> GetImageUrlAsync(string id, string extendDataJson = "")
    {
        if (extendDataJson.IsEmpty())
        {
            return "";
        }
        var extendData = extendDataJson.ToObject<KuGouSearchExtendData>();
        if (extendData == null)
        {
            return "";
        }
        try
        {
            string args = KuGouUtils.GetMusicUrlData(extendData.Hash, extendData.AlbumId);
            string url = $"{UrlBase.KuGou.GetMusic}?{args}";

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            request.Headers.Add("User-Agent", BrowserDefaultHeader.EdgeUserAgent);
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (json.IsEmpty())
            {
                return "";
            }
            var httpResult = json.ToObject<HttpResultBase<MusicDetailHttpResult>>();
            if (httpResult == null)
            {
                return "";
            }
            if (httpResult.status != 1 || httpResult.error_code != 0)
            {
                return "";
            }
            return httpResult.data.img;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "酷狗图片地址获取失败");
            return "";
        }
    }
}