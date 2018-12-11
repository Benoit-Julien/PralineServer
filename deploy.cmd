echo on

mkdir Deployment\%1\Release
mkdir Deployment\%1\Release-Debug

cp %1\bin\Release\* Deployment\%1\Release
cp %1\bin\Release-Debug\* Deployment\%1\Release-Debug

cd Deployment

7z a %1.zip %1/*

cd ..