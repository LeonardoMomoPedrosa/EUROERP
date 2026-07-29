<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:param name="groupId"/>
  <xsl:param name="clientId"/>
  <xsl:decimal-format decimal-separator="," grouping-separator="."/>
  <xsl:key name="rows_name" match="ds" use="NAME" />
  <xsl:template match="/GROUP_REPORT">
    <br/>
    <div align="left">
      <table border="0" width="500">
        <tr>
          <td align="left">
            <b>PRODUTO</b>
          </td>
          <td align="right">
            <b>Total (R$)</b>
          </td>
          <td align="right">
            <b>Peso</b>
          </td>
        </tr>
        <xsl:variable name="products" select="NewDataSet/ds[CID=$clientId and GID=$groupId]"/>
        <xsl:variable name="total" select="sum($products/PRICE)"/>
        <xsl:for-each select="$products">
          <xsl:sort select="sum($products[NAME=current()/NAME]/PRICE)" data-type="number" order="descending"/>
          <xsl:if test="generate-id(.)=generate-id($products[NAME=current()/NAME])">
            <xsl:variable name="total_product" select="sum($products[NAME=current()/NAME]/PRICE)"/>
            <tr>
              <xsl:if test="(position() mod 2 = 1)">
                <xsl:attribute name="bgcolor">#CDCDCD</xsl:attribute>
              </xsl:if>
              <td align="left">
                <xsl:value-of select="NAME"/>
              </td>
              <td align="right">
                <xsl:value-of select="format-number($total_product,'##.##0,00')"/>
              </td>
              <td align="right">
                <xsl:value-of select="format-number(($total_product div $total)*100,'##.##0,00')"/> %
              </td>
            </tr>
          </xsl:if>
        </xsl:for-each>
        <tr>
          <td align="right">
            <b>Total</b>
          </td>
          <td align="right">
            <b>
              R$ <xsl:value-of select="format-number($total,'##.##0,00')"/>
            </b>
          </td>
        </tr>
      </table>
    </div>
  </xsl:template>
</xsl:stylesheet>