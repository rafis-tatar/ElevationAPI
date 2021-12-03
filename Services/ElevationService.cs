namespace ElevationAPI.Services
{
    public class ElevationService
    {
        HttpClient _httpClient;
        IConfiguration _configuration;
        public ElevationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }       
       
        private string GetSrtmTileName(int latDec, int lonDec) =>$"{(latDec > 0 ? 'N' : 'S')}{Math.Abs(latDec):D2}{(lonDec > 0 ? 'E' : 'W')}{Math.Abs(lonDec):D2}";
        private async Task<byte[]> DownloadSrtmFile(string fileName) 
        {
            var name = $"{fileName}.hgt.zip";
            var buff = await _httpClient.GetByteArrayAsync(name);
            SaveSrtmFile(buff, name);
            return buff;
        }
        private void SaveSrtmFile(byte[] buff, string fileName)
        {
            var dir = _configuration["SRTM_PATH"];
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            throw new NotImplementedException();
        }

        private async Task<Stream> GetSrtmStream(string fileName)
        {
            byte[] buff = await LoadSrtmFile(fileName) ?? await DownloadSrtmFile(fileName);            
            if (buff is null) throw new NullReferenceException("Can`t load SRTM file");            
            return new MemoryStream(buff);
        }
        
        private Task<byte[]> LoadSrtmFile(string fileName)
        {
            throw new NotImplementedException();
        }

        

        private int SrtmReadPx(Stream ms, int y, int x)
        {
            int TotalPx = 1201;
            int row = (TotalPx - 1) - y;
            int col = x;
            int pos = (row * TotalPx + col) * 2;
            byte[] buff = new byte[2];
            ms.Position = pos;
            ms.Read(buff, 0, buff.Length);
            var h = (Int16)(buff[0] << 8 | buff[1]);
            return h;
        }

        public async Task<float> GetElevation(double lat, double lon)
        {
            int SecondsPerPx = 3;
            int latDec = (int)lat;
            int lonDec = (int)lon;

            double secondsLat = (lat - latDec) * 60 * 60;
            double secondsLon = (lon - lonDec) * 60 * 60;

            var fileName = GetSrtmTileName(latDec, lonDec);
            var stream = await GetSrtmStream(fileName);        

            //X coresponds to x/y values,
            //everything easter/norhter (< S) is rounded to X.
            //
            //  y   ^
            //  3   |       |   S
            //      +-------+-------
            //  0   |   X   |
            //      +-------+-------->
            // (sec)    0        3   x  (lon)

            //both values are 0-1199 (1200 reserved for interpolating)
            int y = (int)(secondsLat / SecondsPerPx);
            int x = (int)(secondsLon / SecondsPerPx);

            //get norther and easter points
            int[] height = new int[4]{
                SrtmReadPx(stream, y + 1, x),  
                SrtmReadPx(stream, y + 1, x + 1),
                SrtmReadPx(stream, y, x),
                SrtmReadPx(stream, y, x + 1)
            };

            //ratio where X lays
            double dy = (secondsLat % SecondsPerPx) / SecondsPerPx;
            double dx = (secondsLon % SecondsPerPx) / SecondsPerPx;

            // Bilinear interpolation
            // h0------------h1
            // |
            // |--dx-- .
            // |       |
            // |      dy
            // |       |
            // h2------------h3   
            return (float)(height[0] * dy * (1 - dx) +		
                   height[1] * dy * (dx) +
                   height[2] * (1 - dy) * (1 - dx) +
                   height[3] * (1 - dy) * dx);
        }       
        
    }
}