using Proposly.Application.OfferManagement.Responses;

namespace Proposly.Application.Abstractions;

public interface IPdfService
{
    byte[] GenerateOfferPdf(OfferDetailResponse offer);
}
