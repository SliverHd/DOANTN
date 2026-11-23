namespace XepLichGiangDay.Api.Services.ThuatToan.Genetic;

public class ChonLoc
{
    private readonly Random _random;

    public ChonLoc(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    /// Chọn lọc giải đấu (Tournament Selection):
    /// Chọn k cá thể ngẫu nhiên và lấy cá thể tốt nhất theo quy tắc phân tầng:
    /// 1. Ít vi phạm ràng buộc cứng hơn.
    /// 2. Khi cùng số vi phạm cứng: điểm phạt mềm thấp hơn.
    /// </summary>
    public CaThe ChonCaTheChaMe(List<CaThe> quanThe, int kichThuocGiaiDau = 4)
    {
        if (quanThe == null || quanThe.Count == 0)
        {
            throw new InvalidOperationException("Quần thể rỗng.");
        }

        int actualK = Math.Min(kichThuocGiaiDau, quanThe.Count);
        if (actualK < 1) actualK = 1;

        CaThe best = quanThe[_random.Next(quanThe.Count)];

        for (int i = 1; i < actualK; i++)
        {
            var doiThu = quanThe[_random.Next(quanThe.Count)];
            if (doiThu.SoSanhVoi(best) < 0)
            {
                best = doiThu;
            }
        }

        return best;
    }
}

