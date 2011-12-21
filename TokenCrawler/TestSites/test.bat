TokenCrawler.exe /token tostring /url www.sapo.pt
pause
TokenCrawler.exe /token "(\<canvas|createElement\('canvas'\))" /File canvas.txt
pause
TokenCrawler.exe /token "(msapplication|pinify)" /File ie9pin.txt
pause
TokenCrawler.exe /token statcounter /File statcounter.txt
pause
TokenCrawler.exe /token x-ua-compatible /File x-ua-compatible.txt
