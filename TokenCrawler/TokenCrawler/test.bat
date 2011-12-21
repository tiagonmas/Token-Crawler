TokenCrawler.exe /token tostring /url www.sapo.pt
pause
TokenCrawler.exe /token "(\<canvas|createElement\('canvas'\))" /File TestSites/canvas.txt
pause
TokenCrawler.exe /token "(msapplication|pinify)" /File TestSites/ie9pin.txt
pause
TokenCrawler.exe /token statcounter /File TestSites/statcounter.txt
pause
TokenCrawler.exe /token x-ua-compatible /File TestSites/x-ua-compatible.txt
