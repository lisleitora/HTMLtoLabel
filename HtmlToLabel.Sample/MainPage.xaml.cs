using System.Windows.Input;
using HtmlToLabel;
namespace HtmlToLabel.Sample;

public partial class MainPage : ContentPage
{
	
    public MainPage()
	{
		InitializeComponent();
        HtmlToLabel.Convert(pgTitle, @"<span
											style=""
												color:darkblue;
												background-color:yellow;
												font-weight:bold;
												font-size:50pt"">HTML TO LABEL</span>");
    }

    private void OnConverterClicked(object sender, EventArgs e)
	{
        string html = @"<p style=""color:red;font-size:70pt"">Intro</p>
						<p>
							<p style=""color: #FF00FF; font-size: 40px"">there here a link:</p>
							<a target=""_blank"" href=""https://google.com"">LINK</a> Normal text here <b> text bold </b>, <u> text underline </u> <i> text italic</i>
						</p>";
        HtmlToLabel.Convert(pgLabel, html, true, true);
    }
}