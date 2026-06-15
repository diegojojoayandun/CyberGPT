@echo off
echo ========================================
echo  CyberGPT - Iniciando servicios locales
echo ========================================
echo.
echo [1/2] Iniciando ChromaDB en puerto 8000...
start "ChromaDB" cmd /k "chroma run --host localhost --port 8000"

timeout /t 3 /nobreak >nul

echo [2/2] Iniciando Backend .NET en puerto 5000...
start "CyberGPT Backend" cmd /k "cd backend && dotnet run"

timeout /t 3 /nobreak >nul

echo [3/3] Iniciando Frontend Vite en puerto 3000...
start "CyberGPT Frontend" cmd /k "cd frontend && npm run dev"

echo.
echo ✅ Todo iniciado. Abre http://localhost:3000
echo    (Ollama debe estar corriendo por separado)
pause
