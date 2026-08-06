using System;
using TMPro;
using UnityEngine;

public sealed class GuestProfileScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI bioLabel;
    [SerializeField] private TextMeshProUGUI statusLabel;

    public void Configure(
        TextMeshProUGUI configuredNameLabel,
        TextMeshProUGUI configuredTitleLabel,
        TextMeshProUGUI configuredBioLabel,
        TextMeshProUGUI configuredStatusLabel)
    {
        nameLabel = configuredNameLabel;
        titleLabel = configuredTitleLabel;
        bioLabel = configuredBioLabel;
        statusLabel = configuredStatusLabel;
    }

    private void Awake()
    {
        try
        {
            ContentPackage<GuestRecord> guests = ContentLoader.LoadGuests();
            ContentPackage<LineRecord> lines = ContentLoader.LoadLines();
            GuestRecord guest = guests.items[0];
            LineRecord wearyLine = lines.items.Find(line => line.line_id == "line_greet_weary_01");

            if (wearyLine == null)
            {
                throw new InvalidOperationException("weary 인사 대사를 찾지 못했습니다.");
            }

            nameLabel.text = guest.name;
            titleLabel.text = guest.title;
            bioLabel.text = guest.bio;
            statusLabel.text = wearyLine.text;
        }
        catch (Exception exception)
        {
            nameLabel.text = "콘텐츠를 읽지 못했습니다";
            titleLabel.text = "";
            bioLabel.text = exception.Message;
            statusLabel.text = "오류";
            Debug.LogException(exception);
        }
    }
}
