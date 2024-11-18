start /B ngrok http --url=absolute-jaybird-directly.ngrok-free.app 5780 --basic-auth admin:password123  --log=stdout > NUL
pushd ..\AlexaController\bin\Release\net8.0
AlexaController.exe
