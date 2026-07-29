<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:decimal-format decimal-separator="," grouping-separator="."/>
  <xsl:param name="detailPage"/>
  <xsl:param name="first"/>
  <xsl:param name="last"/>
  <xsl:key name="rows_pym" match="ds" use="PSMID" />
  <xsl:key name="rows_group" match="ds" use="GID" />
  <xsl:key name="rows_group_pym" match="ds" use="concat(SA,'_',GROUP)" />
  <xsl:template match="/GROUP_REPORT">
    <br/>
    <div align="left">
      <xsl:variable name="main_total" select="sum(NewDataSet/ds/PRICE)"/>
      <table border="0" width="500">
        <tr>
          <td align="left">
            <b>Vendedor</b>
          </td>
          <td align="right">
            <b>Total (R$)</b>
          </td>
          <td align="right">
            <b>Peso</b>
          </td>
        </tr>
        <xsl:for-each select="NewDataSet/ds[count(. | key('rows_pym', PSMID)[1]) = 1 and (PSMID = 2 or PSMID = 3)]">
          <xsl:variable name="pyms" select="key('rows_pym',PSMID)"/>
          <xsl:variable name="total_pym" select="sum($pyms/PRICE)"/>
          <tr>
            <td bgcolor="#E3E5A3">
              <a href="#">
                <xsl:attribute name="onClick">
                  window.open('<xsl:value-of select="$detailPage"/>psmid=<xsl:value-of select="PSMID"/>&amp;psmn=<xsl:value-of select="PSMN"/>&amp;first=<xsl:value-of select="$first"/>&amp;last=<xsl:value-of select="$last"/>','','height=600,width=800,left=50,top=50,scrollbars=1,menubar=0');return false;
                </xsl:attribute>
                <xsl:value-of select="PSMN"/>
              </a>
            </td>
            <td bgcolor="#E3E5A3" align="right">
              <xsl:value-of select="format-number($total_pym,'##.##0,00')"/>
            </td>
            <td bgcolor="#E3E5A3" align="right">
              <xsl:value-of select="format-number(($total_pym div $main_total)*100,'##.##0,00')"/> %
            </td>
          </tr>
        </xsl:for-each>
        <tr bgcolor="#E3E5A3">
          <td align="right">
            <b>Total</b>
          </td>
          <td align="right">
            <b>
              R$ <xsl:value-of select="format-number($main_total,'##.##0,00')"/>
            </b>
          </td>
          <td></td>
        </tr>
      </table>
    </div>
  </xsl:template>
</xsl:stylesheet>