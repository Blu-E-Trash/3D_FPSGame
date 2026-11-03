using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;

// 보고서용 증거 사진(Docs/Perf/evidence)을 이 컴퓨터 안에서만(localhost) 잠깐 보여 주는 서버.
// 노션 페이지에 사진을 붙일 때만 켜고, 다 쓰면 끈다.
public static class EvidenceServer
{
    private const string Prefix = "http://localhost:8765/";
    private static HttpListener _listener;
    private static Thread _thread;

    [MenuItem("Tools/Perf/Evidence Server/Start")]
    private static void Start()
    {
        if (_listener != null) return;
        string root = Path.GetFullPath("Docs/Perf/evidence");
        _listener = new HttpListener();
        _listener.Prefixes.Add(Prefix);
        _listener.Start();
        _thread = new Thread(() => Serve(root)) { IsBackground = true };
        _thread.Start();
        Debug.Log($"[EvidenceServer] 시작: {Prefix} → {root}");
    }

    [MenuItem("Tools/Perf/Evidence Server/Stop")]
    private static void Stop()
    {
        if (_listener == null) return;
        _listener.Stop();
        _listener.Close();
        _listener = null;
        Debug.Log("[EvidenceServer] 중지");
    }

    private static void Serve(string root)
    {
        while (_listener != null && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = _listener.GetContext(); }
            catch (Exception) { return; }

            HttpListenerResponse res = ctx.Response;
            res.AddHeader("Access-Control-Allow-Origin", "*");
            res.AddHeader("Access-Control-Allow-Private-Network", "true");
            res.AddHeader("Access-Control-Allow-Headers", "*");
            try
            {
                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 204;
                }
                else
                {
                    string name = Path.GetFileName(Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath));
                    string path = Path.Combine(root, name);
                    if (name.EndsWith(".jpg") && File.Exists(path))
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        res.ContentType = "image/jpeg";
                        res.ContentLength64 = bytes.Length;
                        res.OutputStream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        res.StatusCode = 404;
                    }
                }
            }
            catch (Exception) { }
            finally { res.Close(); }
        }
    }
}
