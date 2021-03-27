using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGIS.ShapeFileLib
{
	/// <summary>
	/// ICustomRenderSettings class that returns default RenderSettings values
	/// </summary>
	/// <remarks>
	/// All method on the class are marked as virtual to allow derived classes to 
	/// override only the required custom features
	/// </remarks>
	public class BaseCustomRenderSettings : ICustomRenderSettings
	{
		protected RenderSettings renderSettings;

		/// <summary>
		/// constructs a BaseCusomtRenderSettings object
		/// </summary>
		/// <param name="renderSettings">A RenderSetting object to provide default ICustomRenderSettings values</param>
		public BaseCustomRenderSettings(RenderSettings renderSettings)
		{
			this.renderSettings = renderSettings;
		}

		/// <summary>
		/// virtual UseCustomTooltips method that returns false
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual bool UseCustomTooltips
		{
			get { return false; }
		}

		/// <summary>
		/// virtual UseCustomImageSymbols method that returns false
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual bool UseCustomImageSymbols
		{
			get { return false; }
		}

		/// <summary>
		/// virtual GetRecordFillColor method that returns the default renderSettings.FillColor
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual Color GetRecordFillColor(int recordNumber)
		{
			return this.renderSettings.FillColor;
		}

		/// <summary>
		/// virtual GetRecordFontColor method that returns the default renderSettings.FontColor
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual Color GetRecordFontColor(int recordNumber)
		{
			return this.renderSettings.FontColor;
		}

		/// <summary>
		/// virtual GetRecordImageSymbol method that returns renderSettings.GetImageSymbol()
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual Image GetRecordImageSymbol(int recordNumber)
		{
			return this.renderSettings.GetImageSymbol();
		}

		/// <summary>
		/// virtual GetRecordLabel method that returns the renderSettings.FieldName attribute for the recordNumber
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual string GetRecordLabel(int recordNumber)
		{
			return renderSettings.FieldIndex >= 0 ? renderSettings.DbfReader.GetFields(recordNumber)[renderSettings.FieldIndex].Trim() : "";
		}

		/// <summary>
		/// virtual UseCustomRecordLabels method that returns true if the renderSettings.FieldIndex is >=0 (A valid FieldName has been defined)
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual bool UseCustomRecordLabels
		{
			get { return renderSettings.FieldIndex >= 0; }
		}

		/// <summary>
		/// virtual GetRecordOutlineColor that returns the default  method that returns false
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual Color GetRecordOutlineColor(int recordNumber)
		{
			return renderSettings.OutlineColor;
		}

		/// <summary>
		/// virtual GetRecordTooltip method that returns the default renderSettings TooltipFieldName attribute for the recordNumber
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>

		public virtual string GetRecordToolTip(int recordNumber)
		{
			return renderSettings.UseToolTip && renderSettings.ToolTipFieldIndex >= 0 ? renderSettings.DbfReader.GetFields(recordNumber)[renderSettings.ToolTipFieldIndex].Trim() : "";
		}

		/// <summary>
		/// virtual RenderShape method that returns true
		/// </summary>
		/// <remarks>override to change the default behaviour</remarks>
		public virtual bool RenderShape(int recordNumber)
		{
			return true;
		}

		/// <summary>
		/// virtual GetDirection method that returns 1 if renderSettings.DrawDirectionArrows true, otherwise 0
		/// </summary>
		/// <param name="recordNumber"></param>
		/// <returns>1 or 0</returns>
		/// <remarks>
		/// override to change default behaviour
		/// </remarks>
		public virtual int GetDirection(int recordNumber)
		{
			return renderSettings.DrawDirectionArrows ? 1 : 0;
		}
	}
}
