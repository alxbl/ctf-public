# Mycoportal .NET Track

This is the Mycoportal Track from NorthSec 2022's CTF.

The code is released as-is and instructions to run the infrastructure 
are provided without any additional support.

# Running Locally

**Requirements**
  - `docker` to build the challenge binaries
  - `lxd` and `lxc` to run the challenge environment
  - `ansible` to deploy to the environment


First, you will need to setup a local lxd instance if you do not have one yet:

```sh
sudo systemctl start lxd
sudo systemctl start docker
lxc switch local
sudo lxd init # defaults should be fine here.
```

> **Note**
> If your user is not part of the lxd group on your computer, you may need to run certain `lxc`
> commands as `root`. To add your user to the group, run: `sudo usermod -aG lxd $USER` and re-login.
>
> Your user also needs to be in the `docker` group.


Now that the cluster is up, it should be possible to deploy:

```sh
cd build/ && ./deploy.sh

# This will take a while...

lxc ls

# +---------------------------+---------+------------------------+-----------------------------------------------+-----------------+-----------+
# |           NAME            |  STATE  |          IPV4          |                     IPV6                      |      TYPE       | SNAPSHOTS |
# +---------------------------+---------+------------------------+-----------------------------------------------+-----------------+-----------+
# | ctn-abeaulieu-avatarsvc-1 | RUNNING | 10.18.109.131 (enp5s0) | fd42:f38:1ce8:575:216:3eff:fef6:7610 (enp5s0) | VIRTUAL-MACHINE | 0         |
# +---------------------------+---------+------------------------+-----------------------------------------------+-----------------+-----------+
# | ctn-abeaulieu-avatarsvc-2 | RUNNING | 10.18.109.211 (eth0)   | fd42:f38:1ce8:575:216:3eff:fefd:9615 (eth0)   | CONTAINER       | 0         |
# +---------------------------+---------+------------------------+-----------------------------------------------+-----------------+-----------+

```

# Trying the challenges

The setup does not configure any firewall or network. IPs will be random.
Within the containers, you can use container names to reach them (they should resolve)
Otherwise you may need to update [src/Mycoverse.Web/appsettings.json](src/Mycoverse.Web/appsettings.json) 
to set the right IP/hostname. 

It might be helpful to update `/etc/hosts` to hardcode the container names/urls to their IPv6.

> **Note**
> Some of the binaries do NOT listen on IPv4. That is because NorthSec is purely IPv6.

# Cleaning up local environment

This assumes you want to get rid of lxd as well.

```sh
export REMOTE=local
lxc stop $REMOTE:ctn-abeaulieu-avatarsvc-1
lxc delete $REMOTE:ctn-abeaulieu-avatarsvc-1
lxc stop $REMOTE:ctn-abeaulieu-avatarsvc-2
lxc delete $REMOTE:ctn-abeaulieu-avatarsvc-2

sudo systemctl stop lxd.socket
sudo systemctl stop lxd

# pacman -Rs lxd
```