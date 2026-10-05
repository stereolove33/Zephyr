import base64
import hashlib
import json
import os
from pathlib import Path
import re
import urllib.request

manifest = json.loads(Path("zephyr-app/release-manifest.json").read_text())
repository = os.environ["GITHUB_REPOSITORY"]
token = os.environ["GH_TOKEN"]
output = Path("release-files")
output.mkdir(exist_ok=True)
package = output / manifest["filename"]
if package.parent != output or not re.fullmatch(r"Zephyr-v[0-9.]+\.zip", manifest["filename"]):
    raise ValueError("Invalid package filename")

with package.open("wb") as target:
    for part in manifest["parts"]:
        if not re.fullmatch(r"[a-f0-9]{40}", part["blob"]):
            raise ValueError("Invalid blob identifier")
        request = urllib.request.Request(
            "https://api.github.com/repos/" + repository + "/git/blobs/" + part["blob"],
            headers={"Authorization": "Bearer " + token, "Accept": "application/vnd.github+json"},
        )
        with urllib.request.urlopen(request, timeout=120) as response:
            blob = json.load(response)
        if blob["encoding"] != "base64":
            raise ValueError("Unexpected blob encoding")
        data = base64.b64decode(blob["content"])
        if len(data) != part["size"] or hashlib.sha256(data).hexdigest() != part["sha256"]:
            raise ValueError("Package part verification failed")
        target.write(data)

if package.stat().st_size != manifest["size"]:
    raise ValueError("Package size verification failed")
if hashlib.sha256(package.read_bytes()).hexdigest() != manifest["sha256"]:
    raise ValueError("Package integrity verification failed")
(output / "SHA256SUMS.txt").write_text(manifest["sha256"] + "  " + package.name + "\n")
(output / "RELEASE-NOTES.md").write_bytes(Path("zephyr-app/RELEASE-NOTES.md").read_bytes())
print("Verified", package.name)
