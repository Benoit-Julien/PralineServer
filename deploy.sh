#!/usr/bin/env bash

mkdir -p Deploy/PralineServer/Release
mkdir -p Deploy/PralineServer/Release-Debug
mkdir -p Deploy/PralineNetworkSDK/Release
mkdir -p Deploy/PralineNetworkSDK/Release-Debug

cp PralineServer/bin/Release/* Deploy/PralineServer/Release
cp PralineServer/bin/Release-Debug/* Deploy/PralineServer/Release-Debug
cp PralineNetworkSDK/bin/Release/* Deploy/PralineNetworkSDK/Release
cp PralineNetworkSDK/bin/Release-Debug/* Deploy/PralineNetworkSDK/Release-Debug

cd Deploy

zip -r PralineServer PralineServer
zip -r PralineNetworkSDK PralineNetworkSDK
