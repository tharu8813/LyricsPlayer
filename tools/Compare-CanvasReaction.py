"""Create reference output from the user-specified Playlist Canvas checkout; writes only the given fixture."""
import json
from pathlib import Path
import sys
import subprocess
import threading

import numpy as np

sys.path.insert(0, sys.argv[1])
from app.renderer.python_visualizer import PythonVisualizerRenderer

renderer = PythonVisualizerRenderer(None)
profile = ("bass", 0.06, 0.4, 1.0, 0.0, "linear")
levels = np.zeros((60, 24), dtype=np.float32)
levels[10:15, :4] = 1
step = renderer.music_envelope(levels, 30, profile)
t = np.arange(32000, dtype=np.float64) / 8000
pcm = (12000 * np.exp(-15 * ((t - 0.5) % 0.5)) * np.sin(2 * np.pi * 65 * t)
       * ((t >= 0.5) & (t < 2)) + 10000 * np.sin(2 * np.pi * 1000 * t)
       * ((t >= 2) & (t < 2.5))).astype(np.int16)
bands = renderer._analyze_levels(pcm.astype(np.float32) / 32768, 30, 24, threading.Event())
expected = renderer.music_envelope(bands, 30, profile)
Path(sys.argv[2]).write_text(json.dumps({
    "source_commit": subprocess.check_output(["git", "-C", sys.argv[1], "rev-parse", "HEAD"], text=True).strip(),
    "profile": profile, "step": step.tolist(), "pcm": expected.tolist(),
}, indent=2) + "\n", encoding="utf-8")
print(f"Wrote {len(expected)} PCM reference frames and {len(step)} envelope frames")
