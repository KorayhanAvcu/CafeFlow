using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CafeOrder.WebUI.ModelBinding;

/// <summary>
/// Formdan gelen decimal değerleri sunucunun diline bakmadan okur.
/// Kullanıcı "185,50" (Türkçe) de yazsa, tarayıcının sayı kutusu "185.50" de gönderse sonuç 185,50 olur.
/// Varsayılan binder sunucunun diline göre okuduğu için Türkçe bir sunucuda "185.50" değerini 18550 sanabilirdi.
/// </summary>
public class DecimalModelBinder : IModelBinder
{
    /// <summary>Formdan gelen metni decimal'e çevirir: "185,50" ve "185.50" ikisini de 185,50 okur; geçersizse alana hata ekler.</summary>
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ValueProviderResult result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None)
        {
            return Task.CompletedTask;   // alan hiç gönderilmedi
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        string text = (result.FirstValue ?? "").Trim().Replace(" ", "");

        if (text == "")
        {
            // decimal? alan boş bırakıldıysa null; decimal alan boşsa hata.
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        // Hem virgül hem nokta varsa sondaki ondalık ayırıcıdır ("1.234,50" ya da "1,234.50").
        int lastComma = text.LastIndexOf(',');
        int lastDot = text.LastIndexOf('.');
        if (lastComma > lastDot)
        {
            text = text.Replace(".", "").Replace(',', '.');
        }
        else
        {
            text = text.Replace(",", "");
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Lütfen geçerli bir sayı girin.");
        }
        return Task.CompletedTask;
    }
}

/// <summary>decimal ve decimal? alanlar için DecimalModelBinder'ı devreye sokar (Program.cs'te kayıtlı).</summary>
public class DecimalModelBinderProvider : IModelBinderProvider
{
    /// <summary>Alan decimal veya decimal? ise DecimalModelBinder'ı verir; diğer tiplerde null döner ve MVC varsayılan binder'ı kullanır.</summary>
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        Type type = context.Metadata.UnderlyingOrModelType;
        if (type == typeof(decimal))
        {
            return new DecimalModelBinder();
        }
        return null;
    }
}
