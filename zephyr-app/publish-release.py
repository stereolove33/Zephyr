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

payload = bytearray()
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
    data = base64.b64decode(blob["content"], validate=False)
    if len(data) != part["size"] or hashlib.sha256(data).hexdigest() != part["sha256"]:
        raise ValueError("Package part verification failed")
    payload.extend(data)

base_package = manifest.get("base_package")
if base_package:
    allowed = r"https://github\.com/stereolove33/Zephyr/releases/download/v[0-9]+\.[0-9]+\.[0-9]+/Zephyr-v[0-9]+\.[0-9]+\.[0-9]+\.zip"
    if not re.fullmatch(allowed, base_package["url"]):
        raise ValueError("Invalid base package URL")
    request = urllib.request.Request(base_package["url"], headers={"User-Agent": "Zephyr-release-publisher"})
    with urllib.request.urlopen(request, timeout=120) as response:
        base = response.read(base_package["size"] + 1)
    if len(base) != base_package["size"] or hashlib.sha256(base).hexdigest() != base_package["sha256"]:
        raise ValueError("Base package verification failed")
    with package.open("wb") as target:
        for segment in manifest["segments"]:
            if segment["source"] not in ("base", "patch"):
                raise ValueError("Invalid segment source")
            source = base if segment["source"] == "base" else payload
            offset, size = segment["offset"], segment["size"]
            if type(offset) is not int or type(size) is not int or offset < 0 or size <= 0 or offset + size > len(source):
                raise ValueError("Invalid segment range")
            data = source[offset:offset + size]
            if hashlib.sha256(data).hexdigest() != segment["sha256"]:
                raise ValueError("Segment verification failed")
            target.write(data)
else:
    package.write_bytes(payload)

if package.stat().st_size != manifest["size"]:
    raise ValueError("Package size verification failed")
if hashlib.sha256(package.read_bytes()).hexdigest() != manifest["sha256"]:
    raise ValueError("Package integrity verification failed")
(output / "SHA256SUMS.txt").write_text(manifest["sha256"] + "  " + package.name + "\n")
(output / "RELEASE-NOTES.md").write_bytes(Path("zephyr-app/RELEASE-NOTES.md").read_bytes())
print("Verified", package.name)
