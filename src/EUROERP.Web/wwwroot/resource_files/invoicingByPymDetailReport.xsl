<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:param name="psmid"/>
  <xsl:decimal-format decimal-separator="," grouping-separator="."/>
  <xsl:template match="/GROUP_REPORT">
    <br/>
    <div align="center">
      <table border="0" width="500">
        <tr>
          <td align="left">
            <b>Pedido</b>
          </td>
          <td align="left">
            <b>Cliente</b>
          </td>
          <td align="center">
            <b>Enviado</b>
          </td>
          <td align="left">
            <b>Usuário</b>
          </td>
          <td align="left">
            <b>Vendedor</b>
          </td>
        </tr>
        <xsl:for-each select="NewDataSet/ds[PSMID = $psmid]">
          <tr onmouseover="this.style.backgroundColor = 'yellow';" onmouseout="this.style.backgroundColor = 'white';">
            <td align="left">
              <a href="#">
                <xsl:attribute name="onClick">
                  window.open('/vendas/os/<xsl:value-of select="PKId"/>','','height=500,width=800,left=100,top=50,scrollbars=1,menubar=1');return false;
                </xsl:attribute>
                <img src="../../../imagens/flecha1.jpg" border="0"/>
                <b>
                  # <xsl:value-of select="PKId"/>
                </b>
              </a>
            </td>
            <td align="left">
              <xsl:value-of select="FN"/>
            </td>
            <td align="center">
              <xsl:value-of select="SD"/>
            </td>
            <td align="left">
              <xsl:value-of select="SA"/>
            </td>
            <td align="left">
              <xsl:value-of select="OS"/>
            </td>
          </tr>
        </xsl:for-each>
      </table>
    </div>
  </xsl:template>
</xsl:stylesheet>