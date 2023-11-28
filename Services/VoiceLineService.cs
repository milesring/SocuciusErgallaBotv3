using Microsoft.Extensions.Logging;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocuciusErgallaBotv3.Services
{
    public class VoiceLineService
    {
        private List<string> _voiceLines = new();
        private Random _random = new();
        private readonly ILogger<VoiceLineService> _logger;

        public VoiceLineService(ILogger<VoiceLineService> logger)
        {
            _logger = logger;
            LoadVoiceLines();
        }

        private void LoadVoiceLines()
        {
            var directory = $"{AppDomain.CurrentDomain.BaseDirectory}Resources{Path.DirectorySeparatorChar}Voicelines";
            var files = Directory.GetFiles(directory, "*.mp3");
            _voiceLines.AddRange(files);
            _logger.LogDebug($"Loaded {files.Length} voicelines");
        }

        public string GetRandomVoiceline()
        {
            return _voiceLines[_random.Next(_voiceLines.Count - 1)];
        }
    }
}
