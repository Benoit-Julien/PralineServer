echo on

mkdir Deployment\%1\Release
mkdir Deployment\%1\ReleaseDebug

copy %1\bin\Release\* Deployment\%1\Release
copy %1\bin\ReleaseDebug\* Deployment\%1\ReleaseDebug

cd Deployment

7z a %1.zip %1\*

cd ..